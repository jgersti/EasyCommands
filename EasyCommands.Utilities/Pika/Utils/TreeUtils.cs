using EasyCommands.Utilities.Pika.Memoization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCommands.Utilities.Pika.Utils {
    public static class TreeUtils {
        public static void RenderTreeView(AST.Node node, string input, string indent, bool lastChild, StringBuilder buffer) {
            var cutoff = 80;
            var input_short = input.Substring(node.Start, Math.Min(input.Length - node.Start, Math.Min(node.Count, cutoff)));

            if (input_short.Length == cutoff)
                input_short += "...";

            input_short = StringUtils.EscapeString(input_short);

            buffer
                .Append(indent).Append(lastChild ? "└─" : "├─")
                .Append(node.label).Append(" : ")
                .Append(node.Start).Append("+")
                .Append(node.Count).Append(" : \"")
                .Append(input_short).Append("\"\n");

            node.children?.ForEach((n, i) => RenderTreeView(n, input, indent + (lastChild ? "  " : "│ "), i == node.children.Count - 1, buffer));
        }

        public static void RenderTreeView(Match match, string label, string input, string indent, bool lastChild, StringBuilder buffer) {
            var cutoff = 80;
            var key = match.Key;
            var input_short = input.Substring(key.Start, Math.Min(input.Length - key.Start, Math.Min(match.Count, cutoff)));

            if (input_short.Length == cutoff)
                input_short += "...";

            input_short = StringUtils.EscapeString(input_short);

            var clause = key.Clause;
            var needsParens = Grammar.Meta.NeedToAddParensAroundASTNodeLabels(clause);
            buffer.Append(indent).Append(lastChild ? "└─" : "├─");

            var name = clause.GetRuleNames();
            if (!string.IsNullOrEmpty(name))
                buffer.Append(name).Append(" <- ");

            if (label != null) {
                buffer.Append(label).Append(':');
                if (needsParens)
                    buffer.Append('(');
            }
            buffer.Append(clause);
            if (label != null && needsParens)
                buffer.Append(')');

            buffer
                .Append(" : ").Append(key.Start)
                .Append('+').Append(match.Count)
                .Append(" : \"").Append(input_short)
                .Append("\"\n");

            var subClauseMatches = match.GetSubClauseMatches().ToArray();
            subClauseMatches.ForEach((e, i) => RenderTreeView(e.match, e.label, input, indent + (lastChild ? "  " : "│ "), i == subClauseMatches.Length-1, buffer));
        }

        public static void PrintTreeView(Match match, string input) {
            var buffer = new StringBuilder();
            RenderTreeView(match, null, input, "", true, buffer);
            Console.WriteLine(buffer);
        }
    }
}
