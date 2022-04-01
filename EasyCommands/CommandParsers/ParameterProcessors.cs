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
            bool CanProcess(IToken t);
            bool Process(List<IToken> tokens, int i, out List<IToken> result, List<List<IToken>> branches);
        }

        public abstract class ParameterProcessor<T> : IParameterProcessor where T : class, IToken {
            public int Rank { get; set; }
            public virtual Type GetProcessedTypes() => typeof(T);
            public int CompareTo(IParameterProcessor other) => Rank.CompareTo(other.Rank);
            public bool CanProcess(IToken t) => t is T;
            public abstract bool Process(List<IToken> tokens, int i, out List<IToken> result, List<List<IToken>> branches);
        }

        // ToDo: remove
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

        // ToDo: remove
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

        // ToDo: remove
        public class MultiListProcessor : ParameterProcessor<ListToken> {
            public override bool Process(List<IToken> p, int i, out List<IToken> finalParameters, List<List<IToken>> branches) {
                while (i > 1 && p[i - 1] is ListToken) i--;
                finalParameters = NewList<IToken>(new ListIndexToken(new ListIndexVariable(((ListToken)p[i]).value, EmptyList())));
                p[i] = finalParameters[0];
                return true;
            }
        }

        // ToDo: find a way to make the branching lazy
        public class BranchingProcessor<T> : ParameterProcessor<T> where T : class, IToken {
            public List<ParameterProcessor<T>> processors;

            public BranchingProcessor(params ParameterProcessor<T>[] p) {
                processors = NewList(p);
            }

            public override bool Process(List<IToken> tokens, int i, out List<IToken> result, List<List<IToken>> branches) {
                result = null;
                var eligibleProcessors = processors.Where(processor => processor.CanProcess(tokens[i])).ToList();
                var copy = new List<IToken>(tokens);

                bool processed = false;
                foreach (IParameterProcessor processor in eligibleProcessors) {
                    if (processed) {
                        List<IToken> ignored;
                        var additionalCopy = new List<IToken>(copy);
                        if (processor.Process(additionalCopy, i, out ignored, branches)) {
                            branches.Insert(0, additionalCopy);
                        }
                    } else {
                        processed = processor.Process(tokens, i, out result, branches);
                    }
                }
                return processed;
            }
        }

        static RuleProcessor<BinaryOperandToken> BiOperandProcessor(int tier) =>
            TwoValueRule(Type<BinaryOperandToken>, requiredLeft<VariableToken>(), requiredRight<VariableToken>(),
                (op, left, right) => op.tier == tier && AllSatisfied(left, right),
                (op, left, right) => new VariableToken(new BinaryOperationVariable(op.value, left.value, right.value)));

        // ToDo: rewrite that mess
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

            Validate<SelectorToken> canConvert = p => processors.Exists(x => x.Satisfied() && x != directionProcessor && x != propertyProcessor);
            Apply<SelectorToken> convert = p => {
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
        static IToken ApplyContition(ConditionToken condition, CommandToken metFetcher, ElseToken otherwise, CommandToken notMetFetcher) {
            Command metCommand = metFetcher.value;
            Command notMetCommand = otherwise != null ? notMetFetcher.value : new NullCommand();
            return new CommandToken(condition.swapCommands
                ? new ConditionalCommand(condition.value, metCommand, notMetCommand, condition.alwaysEvaluate)
                : new ConditionalCommand(condition.value, notMetCommand, metCommand, condition.alwaysEvaluate));
        }

        //Rule Processors
        public class RuleProcessor<T> : ParameterProcessor<T> where T : class, IToken {
            public List<IMatch> matches;
            public Validate<T> validate;
            public Apply<T> apply;

            public RuleProcessor(List<IMatch> m, Validate<T> val, Apply<T> app) {
                matches = m;
                validate = val;
                apply = app;
            }

            public override bool Process(List<IToken> tokens, int i, out List<IToken> result, List<List<IToken>> branches) {
                result = null;
                matches.ForEach(m => m.Clear());
                int j = i + 1;
                while (j < tokens.Count) {
                    if (matches.Exists(m => m.Right(tokens[j]))) j++;
                    else break;
                }

                int k = i;
                while (k > 0) {
                    if (matches.Exists(m => m.Left(tokens[k - 1]))) k--;
                    else break;
                }

                T anchor = (T)tokens[i];
                if (!validate(anchor)) return false;

                var product = apply(anchor);
                result = product as List<IToken> ?? (product is IToken ? NewList((IToken)product) : null);
                if (result == null) throw new Exception("Result must be a Token");

                tokens.RemoveRange(k, j - k);
                tokens.InsertRange(k, result);
                return true;
            }
        }

        static RuleProcessor<T> NoValueRule<T>(Supplier<T> type, Apply<T> apply) where T : class, IToken =>
            NoValueRule(type, p => true, apply);
        static RuleProcessor<T> NoValueRule<T>(Supplier<T> type, Validate<T> validate, Apply<T> apply) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(), validate, apply);

        static RuleProcessor<T> OneValueRule<T, U>(Supplier<T> type, Match<U> u, OneValueApply<T, U> apply) where T : class, IToken =>
            OneValueRule(type, u, (p, a) => a.Satisfied(), apply);
        static RuleProcessor<T> OneValueRule<T, U>(Supplier<T> type, Match<U> u, OneValueValidate<T, U> validate, OneValueApply<T, U> apply) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u), p => validate(p, u), p => apply(p, u.GetValue()));

        static RuleProcessor<T> TwoValueRule<T, U, V>(Supplier<T> type, Match<U> u, Match<V> v, TwoValueApply<T, U, V> apply) where T : class, IToken =>
            TwoValueRule(type, u, v, (p, a, b) => AllSatisfied(a, b), apply);
        static RuleProcessor<T> TwoValueRule<T, U, V>(Supplier<T> type, Match<U> u, Match<V> v, TwoValueValidate<T, U, V> validate, TwoValueApply<T, U, V> apply) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u, v), p => validate(p, u, v), p => apply(p, u.GetValue(), v.GetValue()));

        static RuleProcessor<T> ThreeValueRule<T, U, V, W>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, ThreeValueApply<T, U, V, W> apply) where T : class, IToken =>
            ThreeValueRule(type, u, v, w, (p, a, b, c) => AllSatisfied(a, b, c), apply);
        static RuleProcessor<T> ThreeValueRule<T, U, V, W>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, ThreeValueValidate<T, U, V, W> validate, ThreeValueApply<T, U, V, W> apply) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u, v, w), p => validate(p, u, v, w), p => apply(p, u.GetValue(), v.GetValue(), w.GetValue()));

        static RuleProcessor<T> FourValueRule<T, U, V, W, X>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, Match<X> x, FourValueApply<T, U, V, W, X> apply) where T : class, IToken =>
            FourValueRule(type, u, v, w, x, (p, a, b, c, d) => AllSatisfied(a, b, c, d), apply);
        static RuleProcessor<T> FourValueRule<T, U, V, W, X>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, Match<X> x, FourValueValidate<T, U, V, W, X> validate, FourValueApply<T, U, V, W, X> apply) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u, v, w, x), p => validate(p, u, v, w, x), p => apply(p, u.GetValue(), v.GetValue(), w.GetValue(), x.GetValue()));

        //Utility delegates to efficiently create Rule Processors
        public delegate bool Validate<T>(T t);
        delegate bool OneValueValidate<T, U>(T t, Match<U> a);
        delegate bool TwoValueValidate<T, U, V>(T t, Match<U> a, Match<V> b);
        delegate bool ThreeValueValidate<T, U, V, W>(T t, Match<U> a, Match<V> b, Match<W> c);
        delegate bool FourValueValidate<T, U, V, W, X>(T t, Match<U> a, Match<V> b, Match<W> c, Match<X> d);

        public delegate object Apply<T>(T t);
        delegate object OneValueApply<T, U>(T t, U a);
        delegate object TwoValueApply<T, U, V>(T t, U a, V b);
        delegate object ThreeValueApply<T, U, V, W>(T t, U a, V b, W c);
        delegate object FourValueApply<T, U, V, W, X>(T t, U a, V b, W c, X d);
    }
}
