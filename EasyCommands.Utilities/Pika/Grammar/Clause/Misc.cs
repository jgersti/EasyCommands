using EasyCommands.Utilities.Pika.Memoization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCommands.Utilities.Pika.Grammar {
    public class ASTNodeLabel : Clause {
        public string Label { get; }

        public ASTNodeLabel(string label, Clause clause) : base(clause) {
            Label = label;
        }

        public override void DetermineWhetherCanMatchZeroChars() {}
        public override Memoization.Match Match(Table table, Key key, string input)
            => throw new InvalidOperationException($"{nameof(ASTNodeLabel)} node should not be in final grammar.");
        public override string ToString() => ToStringCached = ToStringCached ?? $"{Label}:({SubClauses.First()})";
    }

    public class RuleRef : Clause {
        public string Name { get; set; }

        public RuleRef(string name) : base() {
            Name = name;
        }

        public override void DetermineWhetherCanMatchZeroChars() { }
        public override Memoization.Match Match(Table table, Key key, string input)
            => throw new InvalidOperationException($"{nameof(RuleRef)} node should not be in final grammar.");
        public override string ToString() => ToStringCached = ToStringCached ?? Name;
    }
}
