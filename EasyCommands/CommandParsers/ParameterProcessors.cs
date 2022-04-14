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
            Rule(Type<BinaryOperandToken>, requiredLeft<VariableToken>(), requiredRight<VariableToken>(),
                (op, left, right) => op.tier == tier && AllSatisfied(left, right),
                (op, left, right) => new VariableToken(new BinaryOperationVariable(op.value, left.value, right.value)));

        // ToDo: rewrite that mess
        static RuleProcessor<SelectorToken> BlockCommandProcessor() {
            var assignmentProcessor = eitherList<AssignmentToken>(true);
            //var assignmentProcessor = requiredEither<AssignmentToken>();
            var increaseProcessor = requiredLeft<IncreaseToken>();
            var incrementProcessor = requiredRight<IncrementToken>();
            var variableProcessor = requiredEither<VariableToken>();
            var propertyProcessor = requiredEither<PropertySupplierToken>();
            var directionProcessor = requiredEither<DirectionToken>();
            var reverseProcessor = requiredEither<ReverseToken>();
            var notProcessor = requiredEither<NotToken>();
            var relativeProcessor = requiredRight<RelativeToken>();
            var absoluteProcessor = requiredEither<AbsoluteToken>();
            var processors = NewList<IMatch>(
                assignmentProcessor,
                increaseProcessor,
                incrementProcessor,
                variableProcessor,
                propertyProcessor,
                directionProcessor,
                reverseProcessor,
                notProcessor,
                relativeProcessor,
                absoluteProcessor);

            Validate<SelectorToken> validate = p => processors.Exists(x => x.Satisfied() && x != directionProcessor && x != propertyProcessor);
            Apply<SelectorToken> apply = p => {
                var check = NewList<IMatch>(assignmentProcessor, increaseProcessor, incrementProcessor).Select(m => m.Satisfied());
                var count = processors.Count(x => x.Satisfied()) + (assignmentProcessor.Satisfied() ? assignmentProcessor.GetValue().Count-1 : 0);

                if (check.Count(b => b) > 1)
                    throw new Exception($".:. {string.Join(", ", check)} {assignmentProcessor.GetValue().Count}");

                if (assignmentProcessor.GetValue().Count > 2)
                    throw new Exception($".:. assignment > 2");

                if (count > 5)
                    throw new Exception($".:. matches > 5");

                // --- this get triggered twice by discarded matches
                //if (relativeProcessor.Satisfied() && !variableProcessor.Satisfied())
                //    throw new Exception($".:. relative w/o variable");

                // --- this get triggered twice by discarded matches
                //if (incrementProcessor.Satisfied() && !variableProcessor.Satisfied())
                //    throw new Exception($".:. increment w/o variable");

                if (AllSatisfied(incrementProcessor, relativeProcessor))
                    throw new Exception($".:. increment & relative");

                if (AllSatisfied(incrementProcessor, notProcessor))
                    throw new Exception($".:. increment & not");

                if (AllSatisfied(increaseProcessor, notProcessor))
                    throw new Exception($".:. increase & not");

                if (AllSatisfied(reverseProcessor, notProcessor))
                    throw new Exception($".:. reverse & not");

                if (AllSatisfied(reverseProcessor, variableProcessor))
                    throw new Exception($".:. reverse & variable");

                if (AllSatisfied(reverseProcessor, increaseProcessor))
                    throw new Exception($".:. reverse & increase");

                if (AllSatisfied(reverseProcessor, incrementProcessor))
                    throw new Exception($".:. reverse & increment");

                if (AllSatisfied(increaseProcessor, variableProcessor) && !relativeProcessor.Satisfied())
                    throw new Exception($".:. increase & variable w/o relative");

                // -- should maybe be valid
                if (AllSatisfied(reverseProcessor, assignmentProcessor) && assignmentProcessor.GetValue().Count == 2)
                    throw new Exception($".:. reverse & assignmentx2");

                // -- should be valid
                //if (AllSatisfied(relativeProcessor, assignmentProcessor) && assignmentProcessor.GetValue().Count == 2)
                //    throw new Exception($".:. relative & assignmentx2");

                if (AllSatisfied(relativeProcessor, notProcessor))
                    throw new Exception($".:. relative & not");

                // -- should maybe be valid
                if (AllSatisfied(notProcessor, assignmentProcessor) && assignmentProcessor.GetValue().Count == 2)
                    throw new Exception($".:. not & assignmentx2");

                if (AllSatisfied(incrementProcessor, absoluteProcessor))
                    throw new Exception($".:. increment & absolute");

                if (AllSatisfied(increaseProcessor, absoluteProcessor))
                    throw new Exception($".:. increase & absolute");

                // -- should maybe be valid
                if (AllSatisfied(relativeProcessor, absoluteProcessor))
                    throw new Exception($".:. increment & absolute");

                // --- a few valid and many discarded matches
                //if (absoluteProcessor.Satisfied() && !assignmentProcessor.Satisfied())
                //    throw new Exception($".:. absolute w/o assignment");

                if (AllSatisfied(reverseProcessor, directionProcessor))
                    throw new Exception($".:. reverse & direction");

                if (AllSatisfied(notProcessor, directionProcessor))
                    throw new Exception($".:. not & direction");

                // --- get triggered by 11 discarded matches
                //if (count == 3 && AllSatisfied(absoluteProcessor, propertyProcessor, directionProcessor))
                //    throw new Exception($".:. SPDA");

                if (assignmentProcessor.GetValue().Count == 2 && !absoluteProcessor.Satisfied())
                    throw new Exception($".:. assignmentx2 w/o absolute");

                if (assignmentProcessor.GetValue().Count == 2 && variableProcessor.Satisfied())
                    throw new Exception($".:. assignmentx2 with var");



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

            return new RuleProcessor<SelectorToken>(processors, validate, apply, true);
        }

        // this no rule
        static IToken ApplyCondition(ConditionToken condition, CommandToken metFetcher, ElseToken otherwise, CommandToken notMetFetcher) {
            Command metCommand = metFetcher.value;
            Command notMetCommand = otherwise != null ? notMetFetcher.value : new NullCommand();
            return new CommandToken(condition.swapCommands
                ? new ConditionalCommand(condition.value, notMetCommand, metCommand, condition.alwaysEvaluate)
                : new ConditionalCommand(condition.value, metCommand, notMetCommand, condition.alwaysEvaluate));
        }

        //Rule Processors
        public class RuleProcessor<T> : ParameterProcessor<T> where T : class, IToken {
            public List<IMatch> matches;
            Validate<T> validate;
            Apply<T> apply;
            bool permutation;

            public RuleProcessor(List<IMatch> m, Validate<T> val, Apply<T> app, bool perm = false) {
                matches = m;
                validate = val;
                apply = app;
                permutation = perm;
            }

            public override bool Process(List<IToken> tokens, int i, out List<IToken> result, List<List<IToken>> branches) {
                result = null;
                matches.ForEach(m => m.Clear());

                int j = i+1;
                int k = i;
                if (permutation) {
                    while (j < tokens.Count && matches.Exists(m => m.Right(tokens[j]))) j++;
                    while (k > 0 && matches.Exists(m => m.Left(tokens[k - 1]))) k--;
                } else {
                    for (int n = 0; n < matches.Count; ++n)
                        while (j < tokens.Count && matches[n].Right(tokens[j])) ++j;
                    for (int n = matches.Count - 1; n >= 0; --n)
                        while (k > 0 && matches[n].Left(tokens[k - 1])) --k;
                }

                T anchor = (T)tokens[i];
                if (!validate(anchor)) return false;

                if (matches.Count >= 9) {
                    var count = matches.Count(x => x.Satisfied());

                    // --- this get triggered once by a discarded match
                    //if (matches[0].Satisfied() &&  !(tokens[k] is AssignmentToken))
                    //    throw new Exception($".:. assignment not first");

                    if (matches[0].Satisfied() &&  tokens[j-1] is AssignmentToken)
                        throw new Exception($".:. assignment last");

                    // --- this get triggered twice by discarded matches
                    //if (matches[8].Satisfied() && tokens[j-1] is RelativeToken)
                    //    throw new Exception($".:. relative last");

                    if (matches[1].Satisfied() && !(tokens[k] is IncreaseToken))
                        throw new Exception($".:. increase not first");

                    Func<Func<IToken, bool>, int> FirstIndex = pred => tokens.FindIndex(k, j-k, t => pred(t));
                    Func<Func<IToken, bool>, int> LastIndex = pred => tokens.FindLastIndex(j-1, j-k, t => pred(t));

                    if (AllSatisfied(matches[3], matches[8])) {
                        var r = FirstIndex(t => t is RelativeToken);
                        var v = FirstIndex(t => t is VariableToken);
                        if (r > v)
                            throw new Exception($".:. variable {v} before relative {r}");
                    }

                    if (AllSatisfied(matches[4], matches[8])) {
                        var r = FirstIndex(t => t is RelativeToken);
                        var p = FirstIndex(t => t is PropertySupplierToken);
                        if (p > r)
                            throw new Exception($".:. relative {r} before property {p}");
                    }

                    if (AllSatisfied(matches[5], matches[8])) {
                        var r = FirstIndex(t => t is RelativeToken);
                        var d = FirstIndex(t => t is DirectionToken);
                        if (d > r)
                            throw new Exception($".:. relative {r} before direction {d}");
                    }

                    if (AllSatisfied(matches[3], matches[2])) {
                        var r = FirstIndex(t => t is IncrementToken);
                        var v = FirstIndex(t => t is VariableToken);
                        if (r > v)
                            throw new Exception($".:. variable {v} before increment {r}");
                    }

                    if (AllSatisfied(matches[4], matches[2])) {
                        var r = FirstIndex(t => t is IncrementToken);
                        var p = FirstIndex(t => t is PropertySupplierToken);
                        if (p > r)
                            throw new Exception($".:. increment {r} before property {p}");
                    }

                    if (AllSatisfied(matches[5], matches[2])) {
                        var r = FirstIndex(t => t is IncrementToken);
                        var d = FirstIndex(t => t is DirectionToken);
                        if (d > r)
                            throw new Exception($".:. increment {r} before direction {d}");
                    }

                    if (AllSatisfied(matches[6])) {
                        var r = FirstIndex(t => t is ReverseToken);
                        if (r < i && r != k)
                            throw new Exception($".:. reverse not first when left of selector");
                        // --- maybe
                        if (r > i && r != j - 1)
                            throw new Exception($".:. reverse not last when right of selector");
                    }

                    if (AllSatisfied(matches[0]) && (matches[0] as ListMatch<AssignmentToken>).GetValue().Count == 1) {
                        var r = FirstIndex(t => t is AssignmentToken);
                        if (r > i)
                            throw new Exception($".:. single assignment after selector");
                    }

                    if (AllSatisfied(matches[0], matches[8]) && (matches[0] as ListMatch<AssignmentToken>).GetValue().Count == 2) {
                        //var a1 = FirstIndex(t => t is AssignmentToken);
                        var a2 = LastIndex(t => t is AssignmentToken);
                        var r = FirstIndex(t => t is RelativeToken);
                        if (r < a2)
                            throw new Exception($".:. relative before second assignment");
                    }

                    if (AllSatisfied(matches[0]) && (matches[0] as ListMatch<AssignmentToken>).GetValue().Count == 1) {
                        var r = FirstIndex(t => t is AssignmentToken);
                        if (r > i)
                            throw new Exception($".:. single assignment after selector");
                    }

                    if (AllSatisfied(matches[7])) {
                        var n = FirstIndex(t => t is NotToken);
                        if (n < i && n != k)
                            throw new Exception($".:. 'not' not first when left of selector");
                    }

                    if (AllSatisfied(matches[7]) && (matches[0] as ListMatch<AssignmentToken>).GetValue().Count == 0) {
                        var n = FirstIndex(t => t is NotToken);
                        if (n > i)
                            throw new Exception($".:. 'not' w/o assignment after selector");
                    }

                    if (AllSatisfied(matches[6]) && (matches[0] as ListMatch<AssignmentToken>).GetValue().Count == 0) {
                        var n = FirstIndex(t => t is ReverseToken);
                        if (n > i)
                            throw new Exception($".:. reverse w/o assignment after selector");
                    }

                    if (AllSatisfied(matches[3], matches[7])) {
                        var n = FirstIndex(t => t is NotToken);
                        var v = FirstIndex(t => t is VariableToken);
                        if (n > v)
                            throw new Exception($".:. variable {v} before not {n}");
                    }

                    if (AllSatisfied(matches[3])) {
                        var v = FirstIndex(t => t is VariableToken);
                        // --- this get triggered by discarded matches for transfer commands
                        //if (v < i && !((tokens[v] as VariableToken).value is StaticVariable))
                        //    throw new Exception($".:. variable {v} before selector is not static\n{string.Join(" ", tokens)} {k}:{j}");

                        // --- this get triggered by discarded matches for transfer commands
                        //if (v < i && !((tokens[v] as VariableToken).value.GetValue().returnType == Return.BOOLEAN))
                        //    throw new Exception($".:. variable {v} before selector last is not bool\n{string.Join(" ", tokens)} {k}:{j}");

                        // --- counter: turn on xxx
                        //if (v < i && !((tokens[v] as VariableToken).value.GetValue().AsBool() == false))
                        //    throw new Exception($".:. variable {v} before selector last is not false\n{string.Join(" ", tokens)} {k}:{j}");
                    }

                    //if (AllSatisfied(matches[4], matches[5]) && (matches[0] as ListMatch<AssignmentToken>).GetValue().Count != 2) {
                    //    var p = FirstIndex(t => t is PropertySupplierToken);
                    //    var d = FirstIndex(t => t is DirectionToken);
                    //    if (p < i && d < i && i - Math.Min(p,d) > 2)
                    //        throw new Exception($".:. SPD {i} {p} {d}");
                    //    if (p > i && d > i && Math.Max(p, d) - i > 2)
                    //        throw new Exception($".:. SPD {i} {p} {d}");
                    //}

                    // --- this get triggered by discarded matches for variable assignments, etc
                    //if (AllSatisfied(matches[9])) {
                    //    var a = FirstIndex(t => t is AbsoluteToken);
                    //    if (a == k || a == j-1)
                    //        throw new Exception($".:. absolute first or last");
                    //}

                    if (AllSatisfied(matches[0], matches[8]) && (matches[0] as ListMatch<AssignmentToken>).GetValue().Count == 2) {
                        //var a1 = FirstIndex(t => t is AssignmentToken);
                        var a2 = LastIndex(t => t is AssignmentToken);
                        var r = FirstIndex(t => t is AbsoluteToken);
                        if (r > a2)
                            throw new Exception($".:. absolute after second assignment");
                    }

                    if (AllSatisfied(matches[7], matches[9])) {
                        var n = FirstIndex(t => t is NotToken);
                        var a = FirstIndex(t => t is AbsoluteToken);
                        if (n > i && Math.Abs(n-a) > 1)
                            throw new Exception($".:. 'not' right of selector far from to");
                        if (n < i)
                            throw new Exception($".:. not left of selector with absolute");
                    }

                    if (AllSatisfied(matches[6], matches[9])) {
                        var n = FirstIndex(t => t is ReverseToken);
                        var a = FirstIndex(t => t is AbsoluteToken);
                        if (n > i && a > n)
                            throw new Exception($".:. reverse right of selector before to");
                        if (n < i)
                            throw new Exception($".:. reverse left of selector with absolute");
                        if (n > i && Math.Abs(n - a) > 1)
                            throw new Exception($".:. reverse right of selector far from to");
                    }

                    if (AllSatisfied(matches[0], matches[9]) && (matches[0] as ListMatch<AssignmentToken>).GetValue().Count == 1) {
                        var a = FirstIndex(t => t is AssignmentToken);
                        var r = FirstIndex(t => t is AbsoluteToken);
                        if (r < a)
                            throw new Exception($".:. absolute before single assignment");
                        if (r == a+1)
                            throw new Exception($".:. absolute directly after single assignment");
                        // reformulate
                        //if (r < i)
                        //    throw new Exception($".:. absolute before selector (single assignment)");
                    }

                    if (AllSatisfied(matches[6]) && !matches[9].Satisfied()) {
                        var n = FirstIndex(t => t is ReverseToken);
                        if (n > i)
                            throw new Exception($".:. reverse right of selector without to");
                    }

                    if (AllSatisfied(matches[6], matches[4])) {
                        var n = FirstIndex(t => t is ReverseToken);
                        var p = FirstIndex(t => t is PropertySupplierToken);
                        if (n > i && n < p)
                            throw new Exception($".:. reverse after selector left of propery");
                    }

                    if (AllSatisfied(matches[6], matches[5])) {
                        var n = FirstIndex(t => t is ReverseToken);
                        var d = FirstIndex(t => t is DirectionToken);
                        if (n > i && n < d)
                            throw new Exception($".:. reverse after selector left of direction");
                    }

                    if (AllSatisfied(matches[7], matches[3])) {
                        var r = FirstIndex(t => t is NotToken);
                        var v = FirstIndex(t => t is VariableToken);
                        if (r < i)
                            throw new Exception($".:. 'not' left of selector with var");
                    }

                    // --- not valid
                    //if (AllSatisfied(matches[7], matches[4])) {
                    //    var n = FirstIndex(t => t is NotToken);
                    //    var p = FirstIndex(t => t is PropertySupplierToken);
                    //    if (n > i && n < p)
                    //        throw new Exception($".:. 'not' after selector left of propery");
                    //}

                    if (AllSatisfied(matches[7], matches[5])) {
                        var n = FirstIndex(t => t is NotToken);
                        var d = FirstIndex(t => t is DirectionToken);
                        if (n > i && n < d)
                            throw new Exception($".:. 'not' after selector left of direction");
                    }

                    if (AllSatisfied(matches[3], matches[4], matches[5], matches[9]) && NoneSatisfied(matches[0], matches[1], matches[2], matches[6], matches[7], matches[8])) {
                        var v = FirstIndex(t => t is VariableToken);
                        var p = FirstIndex(t => t is PropertySupplierToken);
                        var d = FirstIndex(t => t is DirectionToken);
                        var a = FirstIndex(t => t is AbsoluteToken);
                        if (v < a && a < i && a < p && a < d)
                            throw new Exception($".:. VA SPD");
                    }

                    if (AllSatisfied(matches[3], matches[4], matches[9]) && NoneSatisfied(matches[0], matches[1], matches[2], matches[5], matches[6], matches[7], matches[8])) {
                        var v = FirstIndex(t => t is VariableToken);
                        var p = FirstIndex(t => t is PropertySupplierToken);
                        var a = FirstIndex(t => t is AbsoluteToken);
                        if (v < a && a < i && a < p)
                            throw new Exception($".:. VA SP");
                    }

                    if (AllSatisfied(matches[3], matches[5], matches[9]) && NoneSatisfied(matches[0], matches[1], matches[2], matches[4], matches[6], matches[7], matches[8])) {
                        var v = FirstIndex(t => t is VariableToken);
                        var p = FirstIndex(t => t is DirectionToken);
                        var a = FirstIndex(t => t is AbsoluteToken);
                        if (v < a && a < i && a < p)
                            throw new Exception($".:. VA SD");
                    }

                    if (AllSatisfied(matches[3], matches[9]) && NoneSatisfied(matches[0], matches[1], matches[2], matches[4], matches[5], matches[6], matches[7], matches[8])) {
                        var v = FirstIndex(t => t is VariableToken);
                        var a = FirstIndex(t => t is AbsoluteToken);
                        if (v < a && a < i)
                            throw new Exception($".:. VA S");
                    }

                    if (AllSatisfied(matches[9], matches[3])) {
                        var a = FirstIndex(t => t is AbsoluteToken);
                        var v = FirstIndex(t => t is VariableToken);
                        if (v > i && a > v)
                            throw new Exception($".:. var between selector and absolute");
                    }

                    if (AllSatisfied(matches[3]) && count == 1) {
                        var v = FirstIndex(t => t is VariableToken);
                        if (i < v)
                            throw new Exception($".:. SV");
                    }

                    // --- to start collecting -> abs var prop var
                    //if (AllSatisfied(matches[3], matches[4]) ) {
                    //    var v = FirstIndex(t => t is VariableToken);
                    //    var p = FirstIndex(t => t is PropertySupplierToken);
                    //    if (i < v && v < p)
                    //        throw new Exception($".:. S ... V ... P");
                    //}

                    // --- increment needs an exception otherwise this should not be valid
                    //if (i == k)
                    //    throw new Exception(".:. selector first"); ;
                }

                var product = apply(anchor);
                result = product as List<IToken> ?? (product is IToken ? NewList((IToken)product) : null);
                if (result == null) throw new Exception("Result must be a Token");

                tokens.RemoveRange(k, j - k);
                tokens.InsertRange(k, result);
                return true;
            }
        }

        static RuleProcessor<T> Rule<T>(Supplier<T> type, Apply<T> apply, bool permutation = false) where T : class, IToken =>
            Rule(type, p => true, apply, permutation);
        static RuleProcessor<T> Rule<T>(Supplier<T> type, Validate<T> validate, Apply<T> apply, bool permutation = false) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(), validate, apply, permutation);

        static RuleProcessor<T> Rule<T, U>(Supplier<T> type, Match<U> u, Apply<T, U> apply, bool permutation = false) where T : class, IToken =>
            Rule(type, u, (p, a) => a.Satisfied(), apply, permutation);
        static RuleProcessor<T> Rule<T, U>(Supplier<T> type, Match<U> u, Validate<T, U> validate, Apply<T, U> apply, bool permutation = false) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u), p => validate(p, u), p => apply(p, u.GetValue()), permutation);

        static RuleProcessor<T> Rule<T, U, V>(Supplier<T> type, Match<U> u, Match<V> v, Apply<T, U, V> apply, bool permutation = false) where T : class, IToken =>
            Rule(type, u, v, (p, a, b) => AllSatisfied(a, b), apply, permutation);
        static RuleProcessor<T> Rule<T, U, V>(Supplier<T> type, Match<U> u, Match<V> v, Validate<T, U, V> validate, Apply<T, U, V> apply, bool permutation = false) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u, v), p => validate(p, u, v), p => apply(p, u.GetValue(), v.GetValue()), permutation);

        static RuleProcessor<T> Rule<T, U, V, W>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, Apply<T, U, V, W> apply, bool permutation = false) where T : class, IToken =>
            Rule(type, u, v, w, (p, a, b, c) => AllSatisfied(a, b, c), apply, permutation);
        static RuleProcessor<T> Rule<T, U, V, W>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, Validate<T, U, V, W> validate, Apply<T, U, V, W> apply, bool permutation = false) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u, v, w), p => validate(p, u, v, w), p => apply(p, u.GetValue(), v.GetValue(), w.GetValue()), permutation);

        static RuleProcessor<T> Rule<T, U, V, W, X>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, Match<X> x, Apply<T, U, V, W, X> apply, bool permutation = false) where T : class, IToken =>
            Rule(type, u, v, w, x, (p, a, b, c, d) => AllSatisfied(a, b, c, d), apply, permutation);
        static RuleProcessor<T> Rule<T, U, V, W, X>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, Match<X> x, Validate<T, U, V, W, X> validate, Apply<T, U, V, W, X> apply, bool permutation = false) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u, v, w, x), p => validate(p, u, v, w, x), p => apply(p, u.GetValue(), v.GetValue(), w.GetValue(), x.GetValue()), permutation);

        static RuleProcessor<T> Rule<T, U, V, W, X, Y>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, Match<X> x, Match<Y> y, Apply<T, U, V, W, X, Y> apply, bool permutation = false) where T : class, IToken =>
            Rule(type, u, v, w, x, y, (p, a, b, c, d, e) => AllSatisfied(a, b, c, d, e), apply, permutation);
        static RuleProcessor<T> Rule<T, U, V, W, X, Y>(Supplier<T> type, Match<U> u, Match<V> v, Match<W> w, Match<X> x, Match<Y> y, Validate<T, U, V, W, X, Y> validate, Apply<T, U, V, W, X, Y> apply, bool permutation = false) where T : class, IToken =>
            new RuleProcessor<T>(NewList<IMatch>(u, v, w, x, y), p => validate(p, u, v, w, x, y), p => apply(p, u.GetValue(), v.GetValue(), w.GetValue(), x.GetValue(), y.GetValue()), permutation);

        //Utility delegates to efficiently create Rule Processors
        public delegate bool Validate<T>(T t);
        public delegate bool Validate<T, U>(T t, Match<U> a);
        public delegate bool Validate<T, U, V>(T t, Match<U> a, Match<V> b);
        public delegate bool Validate<T, U, V, W>(T t, Match<U> a, Match<V> b, Match<W> c);
        public delegate bool Validate<T, U, V, W, X>(T t, Match<U> a, Match<V> b, Match<W> c, Match<X> d);
        public delegate bool Validate<T, U, V, W, X, Y>(T t, Match<U> a, Match<V> b, Match<W> c, Match<X> d, Match<Y> e);

        public delegate object Apply<T>(T t);
        public delegate object Apply<T, U>(T t, U a);
        public delegate object Apply<T, U, V>(T t, U a, V b);
        public delegate object Apply<T, U, V, W>(T t, U a, V b, W c);
        public delegate object Apply<T, U, V, W, X>(T t, U a, V b, W c, X d);
        public delegate object Apply<T, U, V, W, X, Y>(T t, U a, V b, W c, X d, Y e);
    }
}
