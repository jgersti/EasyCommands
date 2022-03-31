using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using EasyCommands.Utilities.Pika.Utils;
using static IngameScript.Program;
using static EasyCommands.Utilities.ObjectDumper.ObjectDumper;

namespace EasyCommands.Utilities {
    class Program {
        static void Main(string[] args) {
            Console.OutputEncoding = Encoding.UTF8;

            //TestExpressionGrammar();
            //TestMetaGrammar();
            //TestSomething('\t', '\n', '\r',' ');
            //TestSomething('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            //TestSomething('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f');
            //PrintParser(@"set $groupName pistons[""Piston L"", ""Piston R""] velocities to rState == 0 ? detachedSpeed : attachedSpeed");
            PrintParser(@"assign myValue to [1, 2, 3][0]");
            //ExecuteSomething();
            //TestPrototypeGrammar("[Assignment][VariableSelector][AmbiguousString][BlockType][Group][OpenBracket][AmbiguousString][ListSeparator][AmbiguousString][CloseBracket][Property][Ignore][AmbiguousString][Comparison][AmbiguousString][TernaryConditionIndicator][AmbiguousString][TernaryConditionSeparator][AmbiguousString];");
        }

        static void ExecuteSomething() {
            foreach (var p in Parser.parameterProcessorsByParameterType) {
                var name = p.Key.GetGenericName();
                Console.WriteLine($"{name}\n{"".PadRight(name.Length, '-')}\n");
                foreach (var s in RulesToString(p))
                    Console.WriteLine(s);
            }
            //Parser.parameterProcessors.Select(RuleToString).ForEach(s => Console.WriteLine(s));
        }

        static void TestExpressionGrammar() {
            string rawGrammar = File.ReadAllText("Resources\\expression.grammar", Encoding.UTF8);

            string rawInput = @"discriminant=b*b-4*a*c;";

            string topRule = "Program";
            string[] recovery = new[] { topRule, "Statement" };

            var grammar = Pika.Grammar.Meta.Parse(rawGrammar);
            var table = grammar.Parse(rawInput);

            ParserInfo.PrintParseResult(topRule, table, recovery, false);
        }
        static void TestMetaGrammar() {
            ParserInfo.PrintClauses(Pika.Grammar.Meta.Grammar);
            Console.WriteLine();
            ParserInfo.PrintRules(Pika.Grammar.Meta.Grammar);
        }
        static void TestPrototypeGrammar(string input) {
            string rawGrammar = File.ReadAllText("Resources\\prototype.grammar", Encoding.UTF8);

            string topRule = "Line";
            string[] recovery = new[] { topRule };

            var grammar = Pika.Grammar.Meta.Parse(rawGrammar);
            var table = grammar.Parse(input);

            ParserInfo.PrintParseResult(topRule, table, recovery, false);
        }

        static void PrintParser(string line) {
            Parser.Logger = Console.WriteLine;
            Parser.FunctionLookup = s => false;

            Lexer.ClearAllState = () => { };
            Lexer.GetState = () => ProgramState.STOPPED;

            Console.WriteLine(line);
            Console.WriteLine();
            var c = Parser.ParseTokens<CommandToken>(Lexer.GetTokens(line));

            //Console.WriteLine(Dump(c));
        }

        static IEnumerable<string> RulesToString(IEnumerable<IParameterProcessor> rules) => rules.Select(RuleToString);
        static string RuleToString(IParameterProcessor processor) =>
            $"{processor.Rank}. {processor.GetType().GetGenericName().Replace("Processor", null)}\n\t{string.Join("\n\t", GetPattern((dynamic)processor))}\n";

        static IEnumerable<string> GetPattern<T>(BranchingProcessor<T> processor) where T : class, IToken => processor.processors.OfType<dynamic>().SelectMany<dynamic, string>(p => GetPattern(p));
        static IEnumerable<string> GetPattern<T>(ParameterProcessor<T> processor) where T : class, IToken => new[] { "Pattern: <null>" };
        static IEnumerable<string> GetPattern<T>(RuleProcessor<T> processor) where T : class, IToken {
            var payload = processor.processors;

            var either = payload.OfType<dynamic>().Where(p => p.left && p.right).Cast<object>();
            var left = payload.OfType<dynamic>().Where(p => p.left).Select<dynamic, string>(p => either.Contains((object)p) ? $"~{GetParameter(p)}" : GetParameter(p));
            var right = payload.OfType<dynamic>().Where(p => p.right).Select<dynamic, string>(p => either.Contains((object)p) ? $"~{GetParameter(p)}" : GetParameter(p));

            var pattern = new[] { "Pattern:" }
                .Concat(left.Reverse())
                .Concat(new[] { $"^{typeof(T).GetGenericName()}" })
                .Concat(right);

            return new[] { string.Join(" ", pattern) };
        }

        static string GetParameter<T>(Match<T> processor) => $"{typeof(T).GetGenericName()}";
        static string GetParameter<T>(OptionalMatch<T> processor) => $"{typeof(T).GetGenericName()}?";
        static string GetParameter<T>(ListMatch<T> processor) => processor.required ? $"{typeof(T).GetGenericName()}+" : $"{typeof(T).GetGenericName()}*";
    }

    public static class MyExtensionMethods {

        public static IEnumerable<string> AsEnumerable(this string s) => Enumerable.Repeat(s, 1);
        public static string GetGenericName(this Type type, int? depth = null) =>
            type.IsGenericType && (depth == null || depth >= 0)
            ? $"{type.Name.Substring(0, type.Name.IndexOf('`'))}<{String.Join(",", type.GetGenericArguments().Select(t => t.GetGenericName(depth == null ? null : depth-1)))}>"
            : type.Name;
    }
}
