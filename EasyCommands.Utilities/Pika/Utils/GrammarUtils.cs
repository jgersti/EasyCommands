using EasyCommands.Utilities.Pika.Grammar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCommands.Utilities.Pika.Utils {
    public static class GrammarUtils {
        // .Traverse(c => c.SubClauses, c => c.Clause).Distinct().OfType<Terminal>()
        static void FindTerminals(Clause clause, HashSet<Clause> visited, List<Clause> terminals) {
            if (visited.Add(clause)) {
                if (clause is Terminal)
                    terminals.Add(clause);
                else
                    clause.SubClauses.ForEach(c => FindTerminals(c.Clause, visited, terminals));
            }
        }

        // .Traverse(c => c.SubClauses, c => c.Clause).Distinct().Reverse()
        static void FindReachableClauses(Clause clause, HashSet<Clause> visited, List<Clause> reverseTopologicalOrder) {
            if (visited.Add(clause)) {
                clause.SubClauses.ForEach(c => FindReachableClauses(c.Clause, visited, reverseTopologicalOrder));
                reverseTopologicalOrder.Add(clause);
            }
        }

        static void FindCycleHeadClauses(Clause clause, ISet<Clause> discovered, ISet<Clause> finished, ISet<Clause> cycleHeads) {
            if (clause is RuleRef)
                throw new ArgumentException($"There should not be any {nameof(RuleRef)} nodes left in the grammar");

            discovered.Add(clause);
            foreach(var subClause in clause.SubClauses.Select(c => c.Clause)) {
                if (discovered.Contains(subClause))
                    cycleHeads.Add(subClause);
                else if (!finished.Contains(subClause))
                    FindCycleHeadClauses(subClause, discovered, finished, cycleHeads);
            }
            discovered.Remove(clause);
            finished.Add(clause);
        }

        public static IEnumerable<Clause> FindClauseTopologigalSortOrder(Rule top, IEnumerable<Rule> allRules, IEnumerable<Clause> lowestPrecedence) {
            var allClausesUnsorted = new List<Clause>();
            var topLevelVisited = new HashSet<Clause>();

            if (top != null) {
                allClausesUnsorted.Add(top.LabeledClause.Clause);
                topLevelVisited.Add(top.LabeledClause.Clause);
            }

            allRules
                .Select(r => r.LabeledClause.Clause)
                .ForEach(c => FindReachableClauses(c, topLevelVisited, allClausesUnsorted));

            var topLevelClauses = allClausesUnsorted
                .Except(allClausesUnsorted.SelectMany(c => c.SubClauses).Select(c => c.Clause))
                .ToArray();

            var roots = new List<Clause>(topLevelClauses);
            roots.AddRange(lowestPrecedence);

            var cyclesDiscovered = new HashSet<Clause>();
            var cyclesFinished = new HashSet<Clause>();
            var cycleHeads = new HashSet<Clause>();

            topLevelClauses
                .ForEach(c => FindCycleHeadClauses(c, cyclesDiscovered, cyclesFinished, cycleHeads));
            allRules
                .Select(r => r.LabeledClause.Clause)
                .ForEach(c => FindCycleHeadClauses(c, cyclesDiscovered, cyclesFinished, cycleHeads));

            roots.AddRange(cycleHeads);

            var terminalsVisited = new HashSet<Clause>();
            var terminals = new List<Clause>();
            allRules
                .Select(r => r.LabeledClause.Clause)
                .ForEach(c => FindTerminals(c, terminalsVisited, terminals));


            var allClauses = new List<Clause>(terminals);
            var reachableVisited = new HashSet<Clause>(terminals);
            roots
                .ForEach(c => FindReachableClauses(c, reachableVisited, allClauses));

            allClauses.ForEach((c, i) => c.Rank = i);
            return allClauses;
        }

        public static void CheckNoRefCycles(Clause clause, string name, ISet<Clause> visited) {
            // replace with traversal
            if (visited.Add(clause))
                clause.SubClauses.Select(c => c.Clause).ForEach(c => CheckNoRefCycles(c, name, visited));
            else
                throw new ArgumentException($"Rules should not contain cycles when they are created: {name}");
            visited.Remove(clause);
        }

        // clause.Traverse(c => c.SubClauses, c => c.Clause).OfType<RuleRef>.Count(r => r.Equals(ruleNameWithoutPrecedence));
        static int CountRuleSelfReferences(Clause clause, string ruleNameWithoutPrecedence) {
            if ((clause as RuleRef)?.Name.Equals(ruleNameWithoutPrecedence) ?? false)
                return 1;
            else
                return clause.SubClauses.Select(c => c.Clause).Aggregate(0, (i, c) => i += CountRuleSelfReferences(c, ruleNameWithoutPrecedence));
        }

        static int RewriteSelfReferences(Clause clause, Associativity?  associativity, int numSelfRefsSoFar,
            int numSelfRefs, string SelfRefRuleName, bool highestPrec, string currPrecRuleName, string nextHighestPrecRuleName) {
            if (numSelfRefsSoFar < numSelfRefs) {
                foreach (var subClause in clause.SubClauses) {
                    if (subClause.Clause is RuleRef) {
                        if((subClause.Clause as RuleRef).Name.Equals(SelfRefRuleName)) {
                            if (numSelfRefs >= 2)
                                subClause.Clause = new RuleRef(
                                    (associativity == Associativity.LEFT && numSelfRefsSoFar == 0)
                                    || (associativity == Associativity.RIGHT && numSelfRefsSoFar == numSelfRefs - 1)
                                        ? currPrecRuleName
                                        : nextHighestPrecRuleName);
                            else if (!highestPrec) {
                                (subClause.Clause as RuleRef).Name = currPrecRuleName;
                                subClause.Clause = new First(subClause.Clause, new RuleRef(nextHighestPrecRuleName));
                            } else
                                (subClause.Clause as RuleRef).Name = nextHighestPrecRuleName;
                            ++numSelfRefsSoFar;
                        }
                    } else
                        numSelfRefsSoFar = RewriteSelfReferences(subClause.Clause, associativity, numSelfRefsSoFar, numSelfRefs, SelfRefRuleName, highestPrec, currPrecRuleName, nextHighestPrecRuleName);
                    subClause.Clause.ToStringCached = null;
                }
            }
            return numSelfRefsSoFar;
        }

        public static void HandlePrecedence(string ruleNameWithoutPrecedence, IEnumerable<Rule> rules,
            List<Clause> lowestPrecedenceClauses, Dictionary<string, string> ruleNameToLowestPrecedenceLevelRuleName) {
            SortedDictionary<int, Rule> precedenceToRule;
            try {
                precedenceToRule = new SortedDictionary<int, Rule>(rules.ToDictionary(r => r.Precedence));
            } catch (ArgumentException e) {
                throw new ArgumentException($"Multiple rules with name {ruleNameWithoutPrecedence}", e);
            }

            var numPrecedenceLevels = precedenceToRule.Count;

            precedenceToRule.Values
                .ForEach(r => r.Name += $"[{r.Precedence}]");

            Enumerable.Range(0, numPrecedenceLevels).ForEach(i => {
                var rule = precedenceToRule.Values.ElementAt(i);
                var numSelfRefs = CountRuleSelfReferences(rule.LabeledClause.Clause, ruleNameWithoutPrecedence);
                var currPrecRuleName = rule.Name;
                var nextHighestPrecRuleName = precedenceToRule.Values.ElementAt((i+1) % numPrecedenceLevels).Name;
                var highestPrecedence = i == numPrecedenceLevels-1;

                if (numSelfRefs >= 1)
                    RewriteSelfReferences(rule.LabeledClause.Clause, rule.Associativity, 0, numSelfRefs, ruleNameWithoutPrecedence,
                        highestPrecedence, currPrecRuleName, nextHighestPrecRuleName);

                if (!highestPrecedence) {
                    var first = new First(rule.LabeledClause.Clause, new RuleRef(nextHighestPrecRuleName));
                    first.SubClauses.First().Label = rule.LabeledClause.Label;
                    rule.LabeledClause.Label = null;
                    rule.LabeledClause.Clause = first;
                }
            });

            var lowestPrecRule = precedenceToRule.Values.First();
            lowestPrecedenceClauses.Add(lowestPrecRule.LabeledClause.Clause);
            ruleNameToLowestPrecedenceLevelRuleName[ruleNameWithoutPrecedence] = lowestPrecRule.Name;
        }

        public static Clause Intern(Clause clause, Dictionary<string, Clause> toStringToClause) {
            clause.SubClauses.ForEach(c => c.Clause = Intern(c.Clause, toStringToClause));

            var str = clause.ToString();
            if (!toStringToClause.TryGetValue(str, out Clause old))
                toStringToClause[str] = clause;

            return old ?? clause;
        }

        public static void ResolveRuleRefs(AST.LabeledClause labeledClause, Dictionary<string, Rule> ruleNameToRule,
            Dictionary<string, string> ruleNameToLowestPrecedenceLevelRuleName, ISet<Clause> visited) {
            if (labeledClause.Clause is RuleRef) {
                var currLabeledClause = labeledClause;
                var visitedClauses = new HashSet<Clause>();
                while (currLabeledClause.Clause is RuleRef) {
                    if (!visitedClauses.Add(currLabeledClause.Clause)) {
                        throw new ArgumentException($"Reached toplevel RuleRef cycle: {currLabeledClause.Clause}");
                    }
                    var name = ((RuleRef)currLabeledClause.Clause).Name;

                    ruleNameToLowestPrecedenceLevelRuleName.TryGetValue(name, out string lowestPrecRuleName);
                    ruleNameToRule.TryGetValue(lowestPrecRuleName ?? name ,out Rule rule);

                    if (rule == null)
                        throw new ArgumentException($"Unknown rule name: {name}");
                    currLabeledClause = rule.LabeledClause;
                }

                labeledClause.Clause = currLabeledClause.Clause;
                labeledClause.Label = labeledClause.Label ?? currLabeledClause.Label;
            } else {
                // .Traverse(c => c.Clause.SubClauses).Distinct()
                if (visited.Add(labeledClause.Clause))
                    labeledClause.Clause.SubClauses.ForEach(c => ResolveRuleRefs(c, ruleNameToRule, ruleNameToLowestPrecedenceLevelRuleName, visited));
            }
        }

    }
}
