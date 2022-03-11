using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EasyCommands.Utilities.Pika.Grammar {
    [DebuggerDisplay("{ToString(),nq}", Type = "{GetType().Name,nq}")]
    public abstract class Clause : IComparable<Clause> {
        public List<Rule> rules;
        public List<Clause> seedParentClauses = new List<Clause>();

        public IEnumerable<AST.LabeledClause> SubClauses { get; }
        public int Rank { get; set; }
        public bool CanMatchZeroChars { get; protected set; }

        public string ToStringCached { get; set; }
        private string ToStringWithRuleNameCached { get; set; }

        protected Clause(params Clause[] subClauses) {
            if (subClauses.Any() && subClauses.First() is Nothing)
                throw new ArgumentException($"{nameof(Nothing)} cannot be the first subclause of any clause");

            SubClauses = subClauses
                .Select(c => new AST.LabeledClause((c as ASTNodeLabel)?.SubClauses.First().Clause ?? c, (c as ASTNodeLabel)?.Label)).ToArray();
        }

        public int CompareTo(Clause other) => Rank - other.Rank;

        public void RegisterRule(Rule rule) => (rules = rules ?? new List<Rule>()).Add(rule);
        public void UnregisterRule(Rule rule) => rules?.Remove(rule);

        public virtual void AddAsSeedParentClause()
            => SubClauses.Select(c => c.Clause).Distinct().ForEach(c => c.seedParentClauses.Add(this));

        public abstract void DetermineWhetherCanMatchZeroChars();
        public abstract Memoization.Match Match(Memoization.Table table, Memoization.Key key, string input);

        public string GetRuleNames() => string.Join(", ", rules?.Select(r => r.Name).OrderBy(s => s) ?? Enumerable.Empty<string>());

        public abstract override string ToString();
        public string ToStringWithRuleNames() {
            if (ToStringWithRuleNameCached == null) {
                if (rules != null) {
                    var buffer = new StringBuilder();
                    buffer.Append(GetRuleNames()).Append(" <- ");

                    var labels = rules.Select(r => r.LabeledClause.Label).Where(s => s != null);
                    labels.Select(s => s + ":").ForEach(l => buffer.Append(l));

                    var addParens = labels.Any() && Meta.NeedToAddParensAroundASTNodeLabels(this);
                    if (addParens)
                        buffer.Append("(");
                    buffer.Append(this);
                    if (addParens)
                        buffer.Append(")");

                    ToStringWithRuleNameCached = buffer.ToString();
                } else
                    ToStringWithRuleNameCached = ToString();
            }
            return ToStringWithRuleNameCached;
        }
    }
}
