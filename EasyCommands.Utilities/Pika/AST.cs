using EasyCommands.Utilities.Pika.Grammar;
using EasyCommands.Utilities.Pika.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCommands.Utilities.Pika.AST {

    public class Node : IEnumerable<Node> {
        public readonly string label;
        public readonly Clause type;
        public readonly string input;
        public readonly List<Node> children = new List<Node>();

        public int Start { get; }
        public int Count { get; }

        private Node(string label, Clause type, int start, int count, string input) {
            this.label = label;
            this.type = type;
            Start = start;
            Count = count;
            this.input = input;
        }

        public Node(string label, Memoization.Match match, string input) : this(label, match.Key.Clause, match.Key.Start, match.Count, input) {
            AddNodesWithASTNodeLabelsRecursive(this, match, input);
        }

        private static void AddNodesWithASTNodeLabelsRecursive(Node parent, Memoization.Match match, string input) {
            foreach(var e in match.GetSubClauseMatches())
                if (e.label != null)
                    parent.children.Add(new Node(e.label, e.match, input));
                else
                    AddNodesWithASTNodeLabelsRecursive(parent, e.match, input);
        }

        public IEnumerator<Node> GetEnumerator() => children.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => children.GetEnumerator();

        public string GetText() => input.Substring(Start, Count);
        public override string ToString() {
            var buffer = new StringBuilder();
            TreeUtils.RenderTreeView(this, input, "", true, buffer);
            return buffer.ToString();
        }
    }


    public class LabeledClause {
        public Clause Clause { get; set; }
        public string Label { get; set; }

        public LabeledClause(Clause clause, string label) {
            Clause = clause;
            Label = label;
        }

        public override string ToString() => ToStringWithASTNodeLabel(null);
        public string ToStringWithASTNodeLabel(Clause parent) {
            var addParens = parent != null && Grammar.Meta.NeedToAddParensAroundSubClause(parent, Clause);

            if (Label == null && !addParens)
                return Clause.ToString();

            var buffer = new StringBuilder();
            if (Label != null) {
                buffer.Append(Label).Append(":");
                addParens |= Grammar.Meta.NeedToAddParensAroundASTNodeLabels(Clause);
            }

            if (addParens)
                buffer.Append("(");
            buffer.Append(Clause);
            if (addParens)
                buffer.Append(")");

            return buffer.ToString();
        }
    }

    public class LabeledMatch {
        public Memoization.Match Match { get; }
        public string Label { get;  }

        public LabeledMatch(Memoization.Match match, string label) {
            Match = match;
            Label = label;
        }

        public override string ToString() => Label == null ? Match.ToString() : $"{Label}:({Match})";

    }
}
