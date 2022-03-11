using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyCommands.Utilities.Pika.Memoization;
using EasyCommands.Utilities.Pika.Utils;

namespace EasyCommands.Utilities.Pika.Grammar {
    public class Grammar {
        public IEnumerable<Rule> AllRules { get; }
        public IEnumerable<Clause> AllClauses { get; }

        public Dictionary<string, Rule> ruleNameWithPrecedenceToRule;

        public Grammar(IEnumerable<Rule> rules) { // check ordering of clauses ...
            if (!rules?.Any() ?? true)
                throw new ArgumentException("Grammar must consist of atleast one rule");

            var top = rules.First();

            var ruleNameToRules = new Dictionary<string, List<Rule>>();
            foreach(var rule in rules) {
                if (rule.Name == null)
                    throw new ArgumentException("All rules must be named");

                var @ref = rule.LabeledClause.Clause as RuleRef;
                if (@ref?.Name.Equals(rule.Name) ?? false)
                    throw new ArgumentException($"Rule cannot refer toonly itself: {rule.Name}[{rule.Precedence}]");

                if (!ruleNameToRules.TryGetValue(rule.Name, out List<Rule> rulesWithName)) {
                    rulesWithName = new List<Rule>();
                    ruleNameToRules[rule.Name] = rulesWithName;
                }
                rulesWithName.Add(rule);

                GrammarUtils.CheckNoRefCycles(rule.LabeledClause.Clause, rule.Name, new HashSet<Clause>());
            }
            AllRules = rules.ToArray();

            var ruleNameToLowestPrecedenceLevelRuleName = new Dictionary<string, string>();
            var lowestPrecedenceClauses = new List<Clause>();
            ruleNameToRules
                .Where(kv => kv.Value.MoreThan(1))
                .ForEach(kv => GrammarUtils.HandlePrecedence(kv.Key, kv.Value, lowestPrecedenceClauses, ruleNameToLowestPrecedenceLevelRuleName));

            ruleNameWithPrecedenceToRule = AllRules.ToDictionary(r => r.Name);

            AllRules
                .ForEach(r => r.LabeledClause.Clause.RegisterRule(r));

            var ToStringClause = new Dictionary<string, Clause>();
            AllRules
                .ForEach(r => r.LabeledClause.Clause = GrammarUtils.Intern(r.LabeledClause.Clause, ToStringClause));

            var clausesVisitedResolveRuleRef = new HashSet<Clause>();
            AllRules
                .ForEach(r => GrammarUtils.ResolveRuleRefs(r.LabeledClause, ruleNameWithPrecedenceToRule, ruleNameToLowestPrecedenceLevelRuleName, clausesVisitedResolveRuleRef));

            AllClauses = GrammarUtils.FindClauseTopologigalSortOrder(top, AllRules, lowestPrecedenceClauses).ToArray();

            AllClauses
                .ForEach(c => c.DetermineWhetherCanMatchZeroChars());
            AllClauses
                .ForEach(c => c.AddAsSeedParentClause());
        }

        public Table Parse(string input) {
            var table = new Table(this, input);
            var queue = new List<Clause>();

            var terminals = AllClauses.OfType<Terminal>().Where(c => !(c is Nothing));

            for(int start = input.Length-1; start >= 0; --start) {
                queue.Clear();
                queue.AddRange(terminals.OrderBy(c => c));
                while (queue.Count > 0) {
                    var clause = queue[0];
                    queue.RemoveAt(0);
                    var key = new Key(clause, start);
                    var match = clause.Match(table, key, input);
                    table.Add(key, match, queue);
                }
            }
            return table;
        }

        public Rule GetRule(string ruleNameWithPrecedence) {
            if (!ruleNameWithPrecedenceToRule.TryGetValue(ruleNameWithPrecedence, out Rule rule))
                throw new ArgumentException($"Unkown rule name: {ruleNameWithPrecedence}");
            return rule;
        }

        public IEnumerable<Match> GetNonOverlappingMatches(string ruleName, Table table)
            => table.GetNonOverlappingMatches(GetRule(ruleName).LabeledClause.Clause);

        public SortedDictionary<int, Match> GetNAvigableMatches(string ruleName, Table table)
            => table.GetNavigableMatches(GetRule(ruleName).LabeledClause.Clause);
    }
}
