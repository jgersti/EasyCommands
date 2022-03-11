using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IngameScript.Program;
using System.Reflection;

namespace EasyCommands.Utilities {
    class Program {
        static void Main(string[] args) {
            string rawGrammar = @"Program <- Statement+;
Statement <- var:[a-z]+ '=' E ';';
E[4] <- '(' E ')';
E[3] <- num:[0-9]+ / sym:[a-z]+;
E[2] <- arith:(op:'-' E);
E[1,L] <- arith:(E op:('*' / '/') E);
E[0,L] <- arith:(E op:('+' / '-') E);"; //.Replace("\r", null);

            Console.WriteLine(rawGrammar);

            string rawInput = @"discriminant=b*b-4*a*c;"; //.Replace("\r", null);

            string topRule = "Program";
            string[] recovery = new[] { topRule, "Statement" };

            var grammar = Pika.Grammar.Meta.Parse(rawGrammar);
            var table = grammar.Parse(rawInput);

            Pika.Utils.ParserInfo.PrintParseResult(topRule, table, recovery, false);
            //Pika.Utils.ParserInfo.PrintClauses(Pika.Grammar.Meta.Grammar);
            //Console.WriteLine();
            //Pika.Utils.ParserInfo.PrintRules(Pika.Grammar.Meta.Grammar);
        }

        static void ExecuteSomething() {
            foreach (var p in Parser.parameterProcessorsByParameterType) {
                var name = p.Key.GetGenericName();
                Console.WriteLine($"{name}\n{"".PadRight(name.Length, '-')}\n");
                foreach (var s in RulesToString(p))
                    Console.WriteLine(s);
            }
        }

        static IEnumerable<string> RulesToString(IEnumerable<IParameterProcessor> rules) => rules.Select(RuleToString);
        static string RuleToString(IParameterProcessor processor) =>
            $"{processor.Rank}. {processor.GetType().GetGenericName().Replace("Processor", null)}\n\t{string.Join("\n\t", GetPattern((dynamic)processor))}\n";

        static IEnumerable<string> GetPattern<T>(BranchingProcessor<T> processor) where T : class, IToken => processor.processors.OfType<dynamic>().SelectMany<dynamic, string>(p => GetPattern(p));
        static IEnumerable<string> GetPattern<T>(ParameterProcessor<T> processor) where T : class, IToken => new[] { "Pattern: <null>" };
        static IEnumerable<string> GetPattern<T>(RuleProcessor<T> processor) where T : class, IToken {
            var payload = processor.processors;

            var either = payload.OfType<dynamic>().Where(p => p.left && p.right).Cast<object>();
            var left = payload.OfType<dynamic>().Where(p => p.left).Select<dynamic, string>(p => either.Contains((object)p) ? $"*{GetParameter(p)}" : GetParameter(p)).Reverse();
            var right = payload.OfType<dynamic>().Where(p => p.right).Select<dynamic, string>(p => either.Contains((object)p) ? $"*{GetParameter(p)}" : GetParameter(p));

            var pattern = new[] { "Pattern:" }
                .Concat(left)
                .Concat(new[] { $"^{typeof(T).GetGenericName()}" })
                .Concat(right);

            return new[] { string.Join(" ", pattern) };
        }

        static string GetParameter<T>(DataProcessor<T> processor) => $"{typeof(T).GetGenericName()}";
        static string GetParameter<T>(OptionalDataProcessor<T> processor) => $"[{typeof(T).GetGenericName()}]";
        static string GetParameter<T>(ListDataProcessor<T> processor) => processor.required ? $"{{{typeof(T).GetGenericName()}}}" : $"[{{{typeof(T).GetGenericName()}}}]";
    }

    public static class MyExtensionMethods {

        public static IEnumerable<string> AsEnumerable(this string s) => Enumerable.Repeat(s, 1);
        public static string GetGenericName(this Type type, int? depth = null) =>
            type.IsGenericType && (depth == null || depth >= 0)
            ? $"{type.Name.Substring(0, type.Name.IndexOf('`'))}<{String.Join(",", type.GetGenericArguments().Select(t => t.GetGenericName(depth == null ? null : depth-1)))}>"
            : type.Name;
    }
}
