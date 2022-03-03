using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public static class Parser {

            public static ILookup<Type, IParameterProcessor> parameterProcessorsByParameterType;
            public static IList<IParameterProcessor> parameterProcessors = new List<IParameterProcessor>()  {
                new ParenthesisProcessor(),
                new ListProcessor(),

                //SelectorVariableSelectorProcessor
                ThreeValueRule(Type<VariableSelectorToken>, requiredRight<AmbiguousStringToken>(), optionalRight<BlockTypeToken>(), optionalRight<GroupToken>(),
                    (p, selector, blockType, group) => new SelectorToken(new BlockSelector(blockType?.value, group != null, new AmbiguousStringVariable(selector.value)))),
                ThreeValueRule(Type<VariableSelectorToken>, requiredRight<VariableToken>(), optionalRight<BlockTypeToken>(), optionalRight<GroupToken>(),
                    (p, selector, blockType, group) => new SelectorToken(new BlockSelector(blockType?.value, group != null, selector.value))),

                //SelectorProcessor
                new BranchingProcessor<AmbiguousStringToken>(
                    NoValueRule(Type<AmbiguousStringToken>,
                        p => p.subTokens.Count > 0 && p.subTokens[0] is AmbiguousToken,
                        p => p.subTokens),
                    TwoValueRule(Type<AmbiguousStringToken>, optionalRight<BlockTypeToken>(), optionalRight<GroupToken>(),
                            (p, blockType, group) => {
                                if (blockType.GetValue() == null) {
                                    blockType.SetValue(findLast<BlockTypeToken>(p.subTokens));
                                    group.SetValue(findLast<GroupToken>(p.subTokens));
                                }
                                return blockType.GetValue() != null;
                            },
                            (p, blockType, group) => new SelectorToken(new BlockSelector(blockType.value, group != null, p.isImplicit ? new AmbiguousStringVariable(p.value) : GetStaticVariable(p.value)))),
                    NoValueRule(Type<AmbiguousStringToken>,
                        name => PROGRAM.functions.ContainsKey(name.value),
                        name => new FunctionDefinitionToken(() => name.value)),
                    NoValueRule(Type<AmbiguousStringToken>,
                        s => {
                            Primitive primitive;
                            IVariable variable = s.isImplicit ? new AmbiguousStringVariable(s.value) : GetStaticVariable(s.value);
                            if (s.isImplicit && Primitive.TryParse(s.value, out primitive)) variable = GetStaticVariable(primitive.value);
                            return new VariableToken(variable);
                        })),

                NoValueRule(Type<AmbiguousToken>, p => p.alternatives.Count > 0, p => p.alternatives),

                OneValueRule(Type<ListIndexToken>, requiredRight<ListToken>(),
                    (index, list) => new ListIndexToken(new ListIndexVariable(index.value, list.value))),

                OneValueRule(Type<ListToken>, requiredLeft<VariableToken>(),
                    (list, variable) => new ListIndexToken(new ListIndexVariable(variable.value, list.value))),

                //SelfSelectorProcessor
                TwoValueRule(Type<Selftoken>, optionalRight<BlockTypeToken>(), optionalRight<GroupToken>(),
                    (p, blockType, group) => new SelectorToken(new SelfSelector(blockType?.value))),

                //VariableSelectorProcessor
                TwoValueRule(Type<VariableToken>, requiredRight<BlockTypeToken>(), optionalRight<GroupToken>(),
                    (p, blockType, group) => new SelectorToken(new BlockSelector(blockType.value, group != null, p.value))),

                //ListSelectorProcessor
                TwoValueRule(Type<ListIndexToken>, requiredRight<BlockTypeToken>(), optionalRight<GroupToken>(),
                    (p, blockType, group) => new SelectorToken(new BlockSelector(blockType.value, group != null, p.value))),

                //ImplicitAllSelectorProcessor
                OneValueRule(Type<BlockTypeToken>, optionalRight<GroupToken>(),
                    (blockType, group) => new SelectorToken(new BlockTypeSelector(blockType.value))),

                //IndexProcessor
                OneValueRule(Type<IndexToken>, requiredRight<VariableToken>(),
                    (p, var) => new IndexSelectorToken(var.value)),

                //RedundantComparisonProcessor
                //"is not <" => "!<"
                //"is <" => "<"
                //"is not" => !=
                // "not greater than" => <
                OneValueRule(Type<ComparisonToken>, requiredEither<NotToken>(),
                    (p, left) => new ComparisonToken((a, b) => !p.value(a, b))),
                OneValueRule(Type<ComparisonToken>, requiredRight<ComparisonToken>(),
                    (p, right) => new ComparisonToken(right.value)),

                //IndexSelectorProcessor
                OneValueRule(Type<IndexSelectorToken>, requiredLeft<SelectorToken>(),
                    (p, selector) => new SelectorToken(new IndexSelector(selector.value, p.value))),

                //ListProcessors
                OneValueRule(Type<ListToken>, requiredLeft<SelectorToken>(),
                    (list, selector) => new SelectorToken(new IndexSelector(selector.value, new IndexVariable(list.value)))),

                new MultiListProcessor(),

                //IgnoreProcessor
                NoValueRule(Type<IgnoreToken>, p => NewList<IToken>()),

                //FunctionProcessor
                OneValueRule(Type<VariableToken>, requiredLeft<FunctionToken>(),
                    (name, function) => new FunctionDefinitionToken(() => name.value.GetValue().AsString(), function.value)),

                //PropertyProcessor
                NoValueRule(Type<PropertyToken>, p => new PropertySupplierToken(new PropertySupplier(p.value + "", p.Lexeme))),

                //ValuePropertyProcessor
                //Needs to check left, then right, which is opposite the typical checks.
                OneValueRule(Type<ValuePropertyToken>, requiredLeft<VariableToken>(),
                    (p, v) => new PropertySupplierToken(new PropertySupplier(p.value + "", p.Lexeme).WithAttributeValue(v.value))),
                OneValueRule(Type<ValuePropertyToken>, requiredRight<VariableToken>(),
                    (p, v) => new PropertySupplierToken(new PropertySupplier(p.value + "", p.Lexeme).WithAttributeValue(v.value))),

                //AssignmentProcessor
                TwoValueRule(Type<AssignmentToken>, optionalRight<GlobalToken>(), requiredRight<VariableToken>(),
                    (p, g, name) => AllSatisfied(g, name) && (name.GetValue().value is AmbiguousStringVariable),
                    (p, g, name) => new VariableAssignmentToken(((AmbiguousStringVariable)name.value).value, p.value, g != null)),

                //IncreaseProcessor
                OneValueRule(Type<IncreaseToken>, requiredRight<VariableToken>(),
                    (p, name) => name.Satisfied() && (name.GetValue().value is AmbiguousStringVariable),
                    (p, name) => new VariableIncrementToken(((AmbiguousStringVariable)name.value).value, p.value)),

                //IncrementProcessor
                OneValueRule(Type<IncrementToken>, requiredLeft<VariableToken>(),
                    (p, name) => name.Satisfied() && (name.GetValue().value is AmbiguousStringVariable),
                    (p, name) => new VariableIncrementToken(((AmbiguousStringVariable)name.value).value, p.value)),

                //Primitive Processor
                NoValueRule(Type<BooleanToken>, b => new VariableToken(GetStaticVariable(b.value))),

                //ListPropertyAggregationProcessor
                OneValueRule(Type<ListIndexToken>, requiredLeft<PropertyAggregationToken>(),
                    (list, aggregation) => new VariableToken(new ListAggregateVariable(list.value, aggregation.value))),

                //ListComparisonProcessor
                ThreeValueRule(Type<ListIndexToken>, requiredRight<ComparisonToken>(), requiredRight<VariableToken>(), optionalLeft<AggregationModeToken>(),
                    (list, comparison, value, aggregation) => new VariableToken(new ListAggregateConditionVariable(aggregation?.value ?? AggregationMode.ALL, list.value, comparison.value, value.value))),

                //ListIndexAsVariableProcessor
                NoValueRule(Type<ListIndexToken>, list => new VariableToken(list.value)),

                //MinusProcessor
                new BranchingProcessor<MinusToken>(
                    NoValueRule(Type<MinusToken>, minus => new UnaryOperationToken(UnaryOperator.REVERSE)),
                    NoValueRule(Type<MinusToken>, minus => new BinaryOperandToken(BinaryOperator.SUBTRACT, 3))
                ),

                //RoundProcessor
                new BranchingProcessor<RoundToken>(
                    NoValueRule(Type<RoundToken>, round => new BinaryOperandToken(BinaryOperator.ROUND, 1)),
                    NoValueRule(Type<RoundToken>, round => new LeftUnaryOperationToken(UnaryOperator.ROUND)),
                    NoValueRule(Type<RoundToken>, round => new UnaryOperationToken(UnaryOperator.ROUND))
                ),

                //CastProcessor
                new BranchingProcessor<CastToken>(
                    NoValueRule(Type<CastToken>, round => new BinaryOperandToken(BinaryOperator.CAST, 4)),
                    NoValueRule(Type<CastToken>, round => new LeftUnaryOperationToken(UnaryOperator.CAST)),
                    NoValueRule(Type<CastToken>, round => new UnaryOperationToken(UnaryOperator.CAST))
                ),

                //AfterUniOperationProcessor
                OneValueRule(Type<LeftUnaryOperationToken>, requiredLeft<VariableToken>(),
                    (p, df) => new VariableToken(new UnaryOperationVariable(p.value, df.value))),

                //UniOperationProcessor
                OneValueRule(Type<UnaryOperationToken>, requiredRight<VariableToken>(),
                    (p, df) => new VariableToken(new UnaryOperationVariable(p.value, df.value))),

                //VectorProcessor
                FourValueRule(Type<ColonSeparatorToken>, requiredLeft<VariableToken>(), requiredRight<VariableToken>(), requiredRight<ColonSeparatorToken>(), requiredRight<VariableToken>(),
                    (sep1, x, y, sep2, z) => AllSatisfied(x, y, z) && !(x.GetValue().value is VectorVariable || y.GetValue().value is VectorVariable || z.GetValue().value is VectorVariable),
                    (sep1, x, y, sep2, z) => new VariableToken(new VectorVariable { X = x.value, Y = y.value, Z = z.value })),

                //Tier0OperationProcessor
                BiOperandProcessor(0),

                //Tier1OperationProcessor
                BiOperandProcessor(1),

                //Tier2OperationProcessor
                BiOperandProcessor(2),

                //Tier3OperationProcessor
                BiOperandProcessor(3),

                //VariableComparisonProcessor
                TwoValueRule(Type<ComparisonToken>, requiredLeft<VariableToken>(), requiredRight<VariableToken>(),
                    (p, left, right) => new VariableToken(new ComparisonVariable(left.value, right.value, p.value))),

                //NotProcessor
                OneValueRule(Type<NotToken>, requiredRight<VariableToken>(),
                    (p, right) => new VariableToken(new UnaryOperationVariable(UnaryOperator.REVERSE, right.value))),

                //ReverseProcessor
                OneValueRule(Type<ReverseToken>, requiredRight<VariableToken>(),
                    (p, right) => new VariableToken(new UnaryOperationVariable(UnaryOperator.REVERSE, right.value))),

                //AndProcessor
                TwoValueRule(Type<AndToken>, requiredLeft<VariableToken>(), requiredRight<VariableToken>(),
                    (p, left, right) => new VariableToken(new BinaryOperationVariable(BinaryOperator.AND, left.value, right.value))),

                //OrProcessor
                TwoValueRule(Type<OrToken>, requiredLeft<VariableToken>(), requiredRight<VariableToken>(),
                    (p, left, right) => new VariableToken(new BinaryOperationVariable(BinaryOperator.OR, left.value, right.value))),

                //Tier4OperationProcessor
                BiOperandProcessor(4),

                //KeyedVariableProcessor
                TwoValueRule(Type<KeyedVariableToken>, requiredLeft<VariableToken>(), requiredRight<VariableToken>(),
                    (keyed, left, right) => new VariableToken(new KeyedVariable(left.value, right.value))),

                //BlockConditionProcessors
                ThreeValueRule(Type<AndToken>, requiredLeft<BlockConditionToken>(), optionalRight<ThatToken>(), requiredRight<BlockConditionToken>(),
                    (p, left, with, right) => new BlockConditionToken(PROGRAM.AndCondition(left.value, right.value))),
                ThreeValueRule(Type<OrToken>, requiredLeft<BlockConditionToken>(), optionalRight<ThatToken>(), requiredRight<BlockConditionToken>(),
                    (p, left, with, right) => new BlockConditionToken(PROGRAM.OrCondition(left.value, right.value))),

                //ThatBlockConditionProcessor
                FourValueRule(Type<ThatToken>, requiredRight<ComparisonToken>(), optionalRight<PropertySupplierToken>(), optionalRight<DirectionToken>(), optionalRight<VariableToken>(),
                    (with, p, prop, dir, var) => p.Satisfied() && AnyNotNull(var.GetValue(), prop.GetValue()),
                    (with, p, prop, dir, var) => NewList<IToken>(new ThatToken(), new BlockConditionToken(BlockPropertyCondition((prop?.value ?? new PropertySupplier()).WithDirection(dir?.value), new PrimitiveComparator(p.value), var?.value ?? GetStaticVariable(true))))),

                //ConditionalSelectorProcessor
                TwoValueRule(Type<ThatToken>, requiredLeft<SelectorToken>(), requiredRight<BlockConditionToken>(),
                    (p, selector, condition) => new SelectorToken(new ConditionalSelector(selector.value, condition.value))),

                //PropertyAggregationProcessor
                ThreeValueRule(Type<PropertyAggregationToken>, requiredEither<SelectorToken>(), optionalEither<PropertySupplierToken>(), optionalEither<DirectionToken>(),
                    (p, selector, prop, dir) => new VariableToken(new AggregatePropertyVariable(p.value, selector.value, (prop?.value ?? new PropertySupplier()).WithDirection(dir?.value)))),

                //BlockComparisonProcessor
                ThreeValueRule(Type<ComparisonToken>, optionalEither<PropertySupplierToken>(), optionalEither<DirectionToken>(), optionalRight<VariableToken>(),
                    (p, prop, dir, var) => AnyNotNull(var.GetValue(), prop.GetValue()),
                    (p, prop, dir, var) => new BlockConditionToken(BlockPropertyCondition((prop?.value ?? new PropertySupplier()).WithDirection(dir?.value), new PrimitiveComparator(p.value), var?.value ?? GetStaticVariable(true)))),

                //AggregateConditionProcessor
                TwoValueRule(Type<BlockConditionToken>, optionalLeft<AggregationModeToken>(), requiredLeft<SelectorToken>(),
                    (p, aggregation, selector) => new VariableToken(new AggregateConditionVariable(aggregation?.value ?? AggregationMode.ALL, p.value, selector.value))),

                //AggregateSelectorProcessor
                OneValueRule(Type<AggregationModeToken>, requiredRight<SelectorToken>(),
                    (aggregation, selector) => aggregation.value != AggregationMode.NONE && selector.Satisfied(),
                    (aggregation, selector) => selector),

                //RepetitionProcessor
                OneValueRule(Type<RepeatToken>, requiredLeft<VariableToken>(),
                    (p, var) => new RepetitionToken(var.value)),

                //TransferCommandProcessor
                FourValueRule(Type<TransferToken>, requiredLeft<SelectorToken>(), requiredRight<SelectorToken>(), requiredRight<VariableToken>(), optionalRight<VariableToken>(),
                    (t, s1, s2, v1, v2) => new CommandToken(new TransferItemCommand((t.value ? s1 : s2).value, (t.value ? s2 : s1).value, v1.value, v2?.value))),
                FourValueRule(Type<TransferToken>, requiredRight<SelectorToken>(), requiredRight<SelectorToken>(), requiredRight<VariableToken>(), optionalRight<VariableToken>(),
                    (t, s1, s2, v1, v2) => new CommandToken(new TransferItemCommand(s1.value, s2.value, v1.value, v2?.value))),

                //Convert Ambiguous Colon to Ternary Condition Separator
                NoValueRule(Type<ColonSeparatorToken>, b => new TernaryConditionSeparatorToken()),

                //TernaryConditionProcessor
                FourValueRule(Type<TernaryConditionIndicatorToken>, requiredLeft<VariableToken>(), requiredRight<VariableToken>(), requiredRight<TernaryConditionSeparatorToken>(), requiredRight<VariableToken>(),
                    (i, conditionValue, positiveValue, seperator, negativeValue) => new VariableToken(new TernaryConditionVariable() {
                        condition = conditionValue.value,
                        positiveValue = positiveValue.value,
                        negativeValue = negativeValue.value
                    })),

                //IfProcessor
                OneValueRule(Type<IfToken>, requiredRight<VariableToken>(),
                    (p, var) => new ConditionToken(p.inverseCondition ? new UnaryOperationVariable(UnaryOperator.REVERSE, var.value) : var.value, p.alwaysEvaluate, p.swapCommands)),

                //AmbiguousSelectorPropertyProcessor
                new BranchingProcessor<SelectorToken>(
                    BlockCommandProcessor(),
                    TwoValueRule(Type<SelectorToken>, requiredEither<PropertySupplierToken>(), optionalEither<DirectionToken>(),
                        (s, p, d) => new VariableToken(new AggregatePropertyVariable(PROGRAM.SumAggregator, s.value, p.value.WithDirection(d?.value)))),
                    TwoValueRule(Type<SelectorToken>, optionalEither<PropertySupplierToken>(), optionalEither<DirectionToken>(),
                        (s, p, d) => AnyNotNull(p.GetValue(), d.GetValue()),
                        (s, p, d) => {
                            PropertySupplier property = p?.value ?? new PropertySupplier();
                            Direction? direction = d?.value;
                            if (direction == null) property = property.WithPropertyValue(GetStaticVariable(true));
                            return new CommandToken(new BlockCommand(s.value, (b, e) =>
                                b.UpdatePropertyValue(e, property.WithDirection(direction).Resolve(b))));
                        })),

                NoValueRule(Type<RelativeToken>, b => NewList<IToken>()),

                //ListIndexAssignmentProcessor
                TwoValueRule(Type<AssignmentToken>, requiredRight<VariableToken>(), requiredRight<VariableToken>(),
                    (p, list, value) => AllSatisfied(list, value) && list.GetValue().value is ListIndexVariable,
                    (p, list, value) => new CommandToken(new ListVariableAssignmentCommand((ListIndexVariable)list.value, value.value, p.value))),

                //PrintCommandProcessor
                OneValueRule(Type<PrintToken>, requiredRight<VariableToken>(),
                    (p, var) => new CommandToken(new PrintCommand(var.value))),

                //WaitProcessor
                OneValueRule(Type<WaitToken>, optionalRight<VariableToken>(),
                    (p, time) => new CommandToken(new WaitCommand(time?.value ?? GetStaticVariable(0.01666f)))),

                //FunctionCallCommandProcessor
                OneValueRule(Type<FunctionDefinitionToken>, rightList<VariableToken>(false),
                    (p, variables) => new CommandToken(new FunctionCommand(p.switchExecution, p.functionDefinition, variables.Select(v => v.value).ToList()))),

                //VariableAssignmentProcessor
                OneValueRule(Type<VariableAssignmentToken>, requiredRight<VariableToken>(),
                    (p, var) => new CommandToken(new VariableAssignmentCommand(p.variableName, var.value, p.useReference, p.isGlobal))),

                //VariableIncrementProcessor
                OneValueRule(Type<VariableIncrementToken>, optionalRight<VariableToken>(),
                    (increment, variable) => new CommandToken(new VariableIncrementCommand(increment.variableName, increment.value, variable?.value ?? GetStaticVariable(1)))),
                //Handles --i
                OneValueRule(Type<IncrementToken>, requiredRight<VariableToken>(),
                    (p, name) => name.Satisfied() && (name.GetValue().value is AmbiguousStringVariable),
                    (p, name) => new VariableIncrementToken(((AmbiguousStringVariable)name.value).value, p.value)),

                //SendCommandProcessor
                //Note: Message to send always comes first: "send <command> to <tag>" is only supported format
                TwoValueRule(Type<SendToken>, requiredRight<VariableToken>(), requiredRight<VariableToken>(),
                    (p, message, tag) => new CommandToken(new SendCommand(message.value, tag.value))),

                //ListenCommandProcessor
                OneValueRule(Type<ListenToken>, requiredRight<VariableToken>(),
                    (p, var) => new CommandToken(new ListenCommand(var.value, p.value))),

                //IterationProcessor
                OneValueRule(Type<RepetitionToken>, requiredEither<CommandToken>(),
                    (p, command) => new CommandToken(new MultiActionCommand(NewList(command.value), p.value))),

                //QueueProcessor
                OneValueRule(Type<QueueToken>, requiredRight<CommandToken>(),
                    (p, command) => new CommandToken(new QueueCommand(command.value, p.value))),

                //IteratorProcessor
                ThreeValueRule(Type<IteratorToken>, requiredRight<VariableToken>(), requiredRight<VariableToken>(), requiredEither<CommandToken>(),
                    (i, item, list, command) => AllSatisfied(list, command, item) && item.GetValue().value is AmbiguousStringVariable,
                    (i, item, list, command) => new CommandToken(new ForEachCommand(((AmbiguousStringVariable)item.value).value, list.value, command.value))),

                //ConditionalCommandProcessor
                //condition command
                //condition command otherwise command
                ThreeValueRule(Type<ConditionToken>, requiredRight<CommandToken>(), optionalRight<ElseToken>(), optionalRight<CommandToken>(),
                    ConvertConditionalCommand),
                //command condition
                //command condition otherwise command
                ThreeValueRule(Type<ConditionToken>, requiredLeft<CommandToken>(), optionalRight<ElseToken>(), optionalRight<CommandToken>(),
                    ConvertConditionalCommand)
            };

            static Parser() {
                for (int i = 0; i < parameterProcessors.Count; i++)
                    parameterProcessors[i].Rank = i;

                parameterProcessorsByParameterType = parameterProcessors.ToLookup(p => p.GetProcessedTypes());
            }

            /// <summary>
            /// This method inline processes the given list of command parameters.
            /// Any ambiguous parsing branches which were found during processing are also returned as additional entries.
            /// If the desired result (typically a command) does not result from the returned parse, the returned
            /// branches can be re-processed to see if a correct parse results from the alternate branches.
            /// This can continue until no alternate branches are returned.
            /// </summary>
            /// <param name="commandParameters"></param>
            /// <returns></returns>
            public static List<List<IToken>> ApplyRules(List<IToken> commandParameters) {
                var sortedProcessors = new List<IParameterProcessor>();

                var branches = NewList<List<IToken>>();
                AddProcessors(sortedProcessors, commandParameters);

                int processorIndex = 0;
                while (processorIndex < sortedProcessors.Count) {
                    bool revisit = false;
                    bool processed = false;

                    IParameterProcessor current = sortedProcessors[processorIndex];
                    for (int i = commandParameters.Count - 1; i >= 0; i--) {
                        if (current.CanProcess(commandParameters[i])) {
                            List<IToken> finalParameters;
                            if (current.Process(commandParameters, i, out finalParameters, branches)) {
                                AddProcessors(sortedProcessors, finalParameters);
                                processed = true;
                                break;
                            } else
                                revisit = true;
                        }
                    }

                    if (processed) {
                        processorIndex = 0;
                        continue;
                    }

                    if (!revisit)
                        sortedProcessors.RemoveAt(processorIndex);
                    else
                        processorIndex++;
                }

                return branches;
            }

            public static T ParseParameters<T>(List<IToken> parameters) where T : class, IToken {
                var branches = NewList(parameters);
                while (branches.Count > 0) {
                    branches.AddRange(ApplyRules(branches[0]));
                    if (branches[0].Count == 1 && branches[0][0] is T) {
                        return (T)branches[0][0];
                    } else {
                        branches.RemoveAt(0);
                    }
                }
                return null;
            }

            static void AddProcessors(List<IParameterProcessor> sortedProcessors, List<IToken> types) {
                sortedProcessors.AddRange(types
                    .Select(t => t.GetType())
                    .SelectMany(t => parameterProcessorsByParameterType[t])
                    .Except(sortedProcessors));
                sortedProcessors.Sort();
            }
        }
    }
}
