using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        public interface IParameterProcessor : IComparable<IParameterProcessor> {
            int Rank { get; set; }
            Type GetProcessedTypes();
            bool CanProcess(IToken p);
            bool Process(List<IToken> p, int i, out List<IToken> finalParameters, List<List<IToken>> branches);
        }

        public abstract class ParameterProcessor<T> : IParameterProcessor where T : class, IToken {
            public int Rank { get; set; }
            public virtual Type GetProcessedTypes() => typeof(T);
            public int CompareTo(IParameterProcessor other) => Rank.CompareTo(other.Rank);
            public bool CanProcess(IToken p) => p is T;
            public abstract bool Process(List<IToken> p, int i, out List<IToken> finalParameters, List<List<IToken>> branches);
        }

        public class ParenthesisProcessor : ParameterProcessor<OpenParenthesisToken> {
            public override bool Process(List<IToken> p, int i, out List<IToken> finalParameters, List<List<IToken>> branches) {
                finalParameters = null;
                for (int j = i + 1; j < p.Count; j++) {
                    if (p[j] is OpenParenthesisToken) return false;
                    else if (p[j] is CloseParenthesisToken) {
                        var alternateBranches = NewList(p.GetRange(i + 1, j - (i + 1)));
                        p.RemoveRange(i, j - i + 1);

                        while (alternateBranches.Count > 0) {
                            finalParameters = alternateBranches[0];
                            alternateBranches.AddRange(Parser.ApplyRules(finalParameters));
                            alternateBranches.RemoveAt(0);
                            if (finalParameters.Count == 1) break;
                        }

                        foreach (var branch in alternateBranches) {
                            branch.Insert(0, new OpenParenthesisToken());
                            branch.Add(new CloseParenthesisToken());
                            var copy = new List<IToken>(p);
                            copy.InsertRange(i, branch);
                            branches.Add(copy);
                        }

                        p.InsertRange(i, finalParameters);
                        return true;
                    }
                }
                throw new Exception("Missing Closing Parenthesis for Command");
            }
        }

        public class ListProcessor : ParameterProcessor<OpenBracketToken> {
            public override bool Process(List<IToken> p, int i, out List<IToken> finalParameters, List<List<IToken>> branches) {
                finalParameters = null;
                var indexValues = NewList<IVariable>();
                int startIndex = i;
                for (int j = startIndex + 1; j < p.Count; j++) {
                    if (p[j] is OpenBracketToken) return false;
                    else if (p[j] is ListSeparatorToken) {
                        indexValues.Add(ParseVariable(p, startIndex, j));
                        startIndex = j; //set startIndex to next separator
                    } else if (p[j] is CloseBracketToken) {
                        if (j > i + 1) indexValues.Add(ParseVariable(p, startIndex, j)); //dont try to parse []
                        finalParameters = NewList<IToken>(new ListToken(GetStaticVariable(NewKeyedList(indexValues))));
                        p.RemoveRange(i, j - i + 1);
                        p.InsertRange(i, finalParameters);
                        return true;
                    }
                }
                throw new Exception("Missing Closing Bracket for List");
            }

            IVariable ParseVariable(List<IToken> p, int startIndex, int endIndex) {
                var range = p.GetRange(startIndex + 1, endIndex - (startIndex + 1));
                var variable = Parser.ParseTokens<ValueToken<IVariable>>(range);
                if (variable == null) throw new Exception("List Index Values Must Resolve To a Variable");
                return variable.value;
            }
        }

        public class MultiListProcessor : ParameterProcessor<ListToken> {
            public override bool Process(List<IToken> p, int i, out List<IToken> finalParameters, List<List<IToken>> branches) {
                while (i > 1 && p[i - 1] is ListToken) i--;
                finalParameters = NewList<IToken>(new ListIndexToken(new ListIndexVariable(((ListToken)p[i]).value, EmptyList())));
                p[i] = finalParameters[0];
                return true;
            }
        }

        public class BranchingProcessor<T> : ParameterProcessor<T> where T : class, IToken {
            public List<ParameterProcessor<T>> processors;

            public BranchingProcessor(params ParameterProcessor<T>[] p) {
                processors = NewList(p);
            }

            public override bool Process(List<IToken> p, int i, out List<IToken> finalParameters, List<List<IToken>> branches) {
                finalParameters = null;
                var eligibleProcessors = processors.Where(processor => processor.CanProcess(p[i])).ToList();
                var copy = new List<IToken>(p);

                bool processed = false;
                foreach (IParameterProcessor processor in eligibleProcessors) {
                    if (processed) {
                        List<IToken> ignored;
                        var additionalCopy = new List<IToken>(copy);
                        if (processor.Process(additionalCopy, i, out ignored, branches)) {
                            branches.Insert(0, additionalCopy);
                        }
                    } else {
                        processed = processor.Process(p, i, out finalParameters, branches);
                    }
                }
                return processed;
            }
        }

        static RuleProcessor<BinaryOperandToken> BiOperandProcessor(int tier) =>
            TwoValueRule(Type<BinaryOperandToken>, requiredLeft<VariableToken>(), requiredRight<VariableToken>(),
                (operand, left, right) => operand.tier == tier && AllSatisfied(left, right),
                (operand, left, right) => new VariableToken(new BinaryOperationVariable(operand.value, left.value, right.value)));

        static RuleProcessor<SelectorToken> BlockCommandProcessor() {
            var assignmentProcessor = eitherList<AssignmentToken>(true);
            var increaseProcessor = requiredLeft<IncreaseToken>();
            var incrementProcessor = requiredRight<IncrementToken>();
            var variableProcessor = requiredEither<VariableToken>();
            var propertyProcessor = requiredEither<PropertySupplierToken>();
            var directionProcessor = requiredEither<DirectionToken>();
            var reverseProcessor = requiredEither<ReverseToken>();
            var notProcessor = requiredEither<NotToken>();
            var relativeProcessor = requiredRight<RelativeToken>();
            var processors = NewList<IMatch>(
                assignmentProcessor,
                increaseProcessor,
                incrementProcessor,
                variableProcessor,
                propertyProcessor,
                directionProcessor,
                reverseProcessor,
                notProcessor,
                relativeProcessor);

            CanConvert<SelectorToken> canConvert = p => processors.Exists(x => x.Satisfied() && x != directionProcessor && x != propertyProcessor);
            Convert<SelectorToken> convert = p => {
                PropertySupplier propertySupplier = propertyProcessor.Satisfied() ? propertyProcessor.GetValue().value : new PropertySupplier();
                if (directionProcessor.Satisfied()) propertySupplier = propertySupplier.WithDirection(directionProcessor.GetValue().value);

                IVariable variableValue = GetStaticVariable(true);
                if (variableProcessor.Satisfied()) {
                    variableValue = variableProcessor.GetValue().value;
                    propertySupplier = propertySupplier.WithPropertyValue(variableValue);
                }

                if (notProcessor.Satisfied()) {
                    variableValue = new UnaryOperationVariable(UnaryOperator.REVERSE, variableValue);
                    propertySupplier = propertySupplier.WithPropertyValue(variableValue);
                }

                if (incrementProcessor.Satisfied()) {
                    propertySupplier = propertySupplier.WithIncrement(incrementProcessor.GetValue().value);
                } else if (increaseProcessor.Satisfied()) {
                    propertySupplier = propertySupplier.WithIncrement(increaseProcessor.GetValue().value);
                } else if (relativeProcessor.Satisfied()) propertySupplier = propertySupplier.WithIncrement(true);

                Action<IBlockHandler, Object> blockAction;
                if (AllSatisfied(reverseProcessor)) blockAction = (b, e) => b.ReverseNumericPropertyValue(e, propertySupplier.Resolve(b));
                else if (increaseProcessor.Satisfied() || incrementProcessor.Satisfied() || relativeProcessor.Satisfied()) blockAction = (b, e) => b.IncrementPropertyValue(e, propertySupplier.Resolve(b));
                else if (AllSatisfied(directionProcessor)) blockAction = (b, e) => b.UpdatePropertyValue(e, propertySupplier.Resolve(b));
                else blockAction = (b, e) => b.UpdatePropertyValue(e, propertySupplier.WithPropertyValue(variableValue).Resolve(b));

                return new CommandToken(new BlockCommand(p.value, blockAction));
            };

            return new RuleProcessor<SelectorToken>(processors, canConvert, convert);
        }

        // this no rule
        static IToken ConvertConditionalCommand(ConditionToken condition, CommandToken metFetcher, ElseToken otherwise, CommandToken notMetFetcher) {
            Command metCommand = metFetcher.value;
            Command notMetCommand = otherwise != null ? notMetFetcher.value : new NullCommand();
            if (condition.swapCommands) {
                var temp = metCommand;
                metCommand = notMetCommand;
                notMetCommand = temp;
            }
            return new CommandToken(new ConditionalCommand(condition.value, metCommand, notMetCommand, condition.alwaysEvaluate));
        }

        //Rule Processors
        public class RuleProcessor<T> : ParameterProcessor<T> where T : class, IToken {
            public List<IMatch> processors;
            public CanConvert<T> canConvert;
            public Convert<T> convert;

            public RuleProcessor(List<IMatch> proc, CanConvert<T> canConv, Convert<T> conv) {
                processors = proc;
                canConvert = canConv;
                convert = conv;
            }

            public override bool Process(List<IToken> p, int i, out List<IToken> finalParameters, List<List<IToken>> branches) {
                finalParameters = null;
                processors.ForEach(dp => dp.Clear());
                int j = i + 1;
                while (j < p.Count) {
                    if (processors.Exists(dp => dp.Right(p[j]))) j++;
                    else break;
                }

                int k = i;
                while (k > 0) {
                    if (processors.Exists(dp => dp.Left(p[k - 1]))) k--;
                    else break;
                }

                T hook = (T)p[i];
                if (!canConvert(hook)) return false;
                var converted = convert(hook);

                if (converted is IToken)
                    finalParameters = NewList((IToken)converted);
                else if (converted is List<IToken>)
                    finalParameters = (List<IToken>)converted;
                else
                    throw new Exception("Final parameters must be CommandParameter");

                p.RemoveRange(k, j - k);
                p.InsertRange(k, finalParameters);
                return true;
            }
        }

        static RuleProcessor<T> NoValueRule<T>(Supplier<T> type, Convert<T> convert) where T : class, IToken => NoValueRule(type, p => true, convert);

        static RuleProcessor<T> NoValueRule<T>(Supplier<T> type, CanConvert<T> canConvert, Convert<T> convert) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(), canConvert, convert);

        static RuleProcessor<T> OneValueRule<T, U>(Supplier<T> type, Match<U> u, OneValueConvert<T, U> convert) where T : class, IToken =>
            OneValueRule(type, u, (p, a) => a.Satisfied(), convert);

        static RuleProcessor<T> OneValueRule<T, U>(Supplier<T> type, Match<U> u, OneValueCanConvert<T, U> canConvert, OneValueConvert<T, U> convert) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u), p => canConvert(p, u), p => convert(p, u.GetValue()));

        static RuleProcessor<T> TwoValueRule<T, U, V>(Supplier<T> type, Match<U> u, Match<V> v, TwoValueConvert<T, U, V> convert) where T : class, IToken =>
            TwoValueRule(type, u, v, (p, a, b) => AllSatisfied(a, b), convert);

        static RuleProcessor<T> TwoValueRule<T, U, V>(Supplier<T> type, Match<U> u, Match<V> v, TwoValueCanConvert<T, U, V> canConvert, TwoValueConvert<T, U, V> convert) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u, v), p => canConvert(p, u, v), p => convert(p, u.GetValue(), v.GetValue()));

        static RuleProcessor<T> ThreeValueRule<T, U, V, W>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, ThreeValueConvert<T, U, V, W> convert) where T : class, IToken =>
            ThreeValueRule(type, u, v, w, (p, a, b, c) => AllSatisfied(a, b, c), convert);

        static RuleProcessor<T> ThreeValueRule<T, U, V, W>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, ThreeValueCanConvert<T, U, V, W> canConvert, ThreeValueConvert<T, U, V, W> convert) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u, v, w), p => canConvert(p, u, v, w), p => convert(p, u.GetValue(), v.GetValue(), w.GetValue()));

        static RuleProcessor<T> FourValueRule<T, U, V, W, X>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, Match<X> x, FourValueConvert<T, U, V, W, X> convert) where T : class, IToken =>
            FourValueRule(type, u, v, w, x, (p, a, b, c, d) => AllSatisfied(a, b, c, d), convert);

        static RuleProcessor<T> FourValueRule<T, U, V, W, X>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, Match<X> x, FourValueCanConvert<T, U, V, W, X> canConvert, FourValueConvert<T, U, V, W, X> convert) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u, v, w, x), p => canConvert(p, u, v, w, x), p => convert(p, u.GetValue(), v.GetValue(), w.GetValue(), x.GetValue()));

        //Utility delegates to efficiently create Rule Processors
        public delegate bool CanConvert<T>(T t);
        delegate bool OneValueCanConvert<T, U>(T t, Match<U> a);
        delegate bool TwoValueCanConvert<T, U, V>(T t, Match<U> a, Match<V> b);
        delegate bool ThreeValueCanConvert<T, U, V, W>(T t, Match<U> a, Match<V> b, Match<W> c);
        delegate bool FourValueCanConvert<T, U, V, W, X>(T t, Match<U> a, Match<V> b, Match<W> c, Match<X> d);

        public delegate object Convert<T>(T t);
        delegate object OneValueConvert<T, U>(T t, U a);
        delegate object TwoValueConvert<T, U, V>(T t, U a, V b);
        delegate object ThreeValueConvert<T, U, V, W>(T t, U a, V b, W c);
        delegate object FourValueConvert<T, U, V, W, X>(T t, U a, V b, W c, X d);
    }
}
