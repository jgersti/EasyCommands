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
    partial class Program {
        public interface IToken {
            string Lexeme { get; set; }
        }
        public abstract class SimpleToken : IToken {
            public string Lexeme { get; set; }
            public override string ToString() => $"[{this.GetType().Name.Replace("Token", null)}]";
        }

        public abstract class ValueToken<T> : SimpleToken {
            public T value;
            public ValueToken(T v) { value = v; }
        }


        #region Terminals / Input Tokens
        // symbols
        public class IndexToken : SimpleToken { }
        public class VariableSelectorToken : SimpleToken { }
        public class OpenParenthesisToken : SimpleToken { }
        public class CloseParenthesisToken : SimpleToken { }
        public class OpenBracketToken : SimpleToken { }
        public class ListSeparatorToken : SimpleToken { }
        public class CloseBracketToken : SimpleToken { }
        public class KeyedVariableToken : SimpleToken { }
        public class ColonSeparatorToken : SimpleToken { }
        public class TernaryConditionIndicatorToken : SimpleToken { }
        public class MinusToken : SimpleToken { }
        public class IncrementToken : ValueToken<bool> {
            public IncrementToken(bool increase = true) : base(increase) { }
        }


        // symbols with keywords
        public class NotToken : SimpleToken { }
        public class AndToken : SimpleToken { }
        public class OrToken : SimpleToken { }

        // keywords
        public class GroupToken : SimpleToken { }
        public class RepeatToken : SimpleToken { }
        public class ReverseToken : SimpleToken { }
        public class ElseToken : SimpleToken { }
        public class Selftoken : SimpleToken { }
        public class GlobalToken : SimpleToken { }
        public class ThatToken : SimpleToken { }
        public class RoundToken : SimpleToken { }
        public class CastToken : SimpleToken { }
        public class RelativeToken : SimpleToken { }
        public class AbsoluteToken : SimpleToken { }
        public class InToken : SimpleToken { }

        public class FunctionToken : ValueToken<bool> {
            public FunctionToken(bool shouldSwitch) : base(shouldSwitch) { }
        }

        public class BooleanToken : ValueToken<bool> {
            public BooleanToken(bool value) : base(value) { }
        }

        public class DirectionToken : ValueToken<Direction> {
            public DirectionToken(Direction value) : base(value) { }
        }

        public class ValuePropertyToken : ValueToken<ValueProperty> {
            public ValuePropertyToken(ValueProperty value) : base(value) { }
        }

        public class PropertyToken : ValueToken<Property> {
            public PropertyToken(Property value) : base(value) { }
        }

        public class BlockTypeToken : ValueToken<Block> {
            public BlockTypeToken(Block value) : base(value) { }
        }


        // action / control keywords
        public class WaitToken : SimpleToken { }
        public class IteratorToken : SimpleToken { }
        public class PrintToken : SimpleToken { }
        public class SendToken : SimpleToken { }
        public class ListenToken : ValueToken<bool> {
            public ListenToken(bool v) : base(v) { }
        }

        public class QueueToken : ValueToken<bool> {
            public QueueToken(bool async) : base(async) { }
        }

        public class TransferToken : ValueToken<bool> {
            public TransferToken(bool v) : base(v) { }
        }

        public class AssignmentToken : ValueToken<bool> {
            public AssignmentToken(bool reference = false) : base(reference) { }
        }

        public class IncreaseToken : ValueToken<bool> {
            public IncreaseToken(bool increase = true) : base(increase) { }
        }

        public class IfToken : SimpleToken {
            public bool inverseCondition, alwaysEvaluate, swapCommands;

            public IfToken(bool inverse, bool alwaysEval, bool swap) {
                inverseCondition = inverse;
                alwaysEvaluate = alwaysEval;
                swapCommands = swap;
            }
        }

        // misc
        public class AmbiguousToken : SimpleToken {
            public List<IToken> alternatives;

            public AmbiguousToken(params IToken[] commands) {
                alternatives = commands.ToList();
            }
        }
        public class AmbiguousStringToken : ValueToken<String> {
            public List<IToken> subTokens;
            public bool isImplicit;
            public AmbiguousStringToken(String value, bool impl, params IToken[] SubTokens) : base(value) {
                subTokens = SubTokens.ToList();
                isImplicit = impl;
            }
        }
        #endregion

        #region Transformations
        public class TernaryConditionSeparatorToken : SimpleToken { }

        public class UnaryOperationToken : ValueToken<UnaryOperator> {
            public UnaryOperationToken(UnaryOperator value) : base(value) { }
        }

        public class LeftUnaryOperationToken : ValueToken<UnaryOperator> {
            public LeftUnaryOperationToken(UnaryOperator value) : base(value) { }
        }

        public class BinaryOperandToken : ValueToken<BinaryOperator> {
            public int tier;
            public BinaryOperandToken(BinaryOperator value, int t) : base(value) {
                tier = t;
            }
        }

        public class ComparisonToken : ValueToken<PrimitiveComparator> {
            public ComparisonToken(PrimitiveComparator value) : base(value) {
            }
        }
        #endregion

        #region Productions
        public class VariableToken : ValueToken<IVariable> {
            public VariableToken(IVariable value) : base(value) { }
        }

        public class IdentifierToken : ValueToken<string> {
            public IdentifierToken(string value) : base(value) { }
        }

        public class VariableAssignmentToken : SimpleToken {
            public string variableName;
            public bool useReference, isGlobal;

            public VariableAssignmentToken(string variable, bool reference, bool global) {
                variableName = variable;
                useReference = reference;
                isGlobal = global;
            }
        }

        public class ListIndexAssignmentToken : SimpleToken {
            public ListIndexVariable listIndex;
            public bool useReference;

            public ListIndexAssignmentToken(ListIndexVariable variable, bool reference) {
                listIndex = variable;
                useReference = reference;
            }
        }

        public class VariableIncrementToken : ValueToken<bool> {
            public string variableName;
            public VariableIncrementToken(string variable, bool increase = true) : base(increase) {
                variableName = variable;
            }
        }

        public class PropertySupplierToken : ValueToken<PropertySupplier> {
            public PropertySupplierToken(PropertySupplier value) : base(value) { }
        }

        public class ListToken : ValueToken<IVariable> {
            public ListToken(IVariable v) : base(v) { }
        }

        //public class ListXXXToken : ValueToken<IVariable> {
        //    public ListXXXToken(IVariable v) : base(v) {}
        //}

        public class ListIndexToken : ValueToken<ListIndexVariable> {
            public ListIndexToken(ListIndexVariable v) : base(v) { }
        }

        public class IndexSelectorToken : ValueToken<IVariable> {
            public IndexSelectorToken(IVariable value) : base(value) { }
        }

        public class FunctionDefinitionToken : SimpleToken {
            public bool switchExecution;
            public Supplier<string> functionDefinition;

            public FunctionDefinitionToken(Supplier<string> definition, bool shouldSwitch = false) {
                switchExecution = shouldSwitch;
                functionDefinition = definition;
            }
        }

        public class ConditionToken : ValueToken<IVariable> {
            public bool alwaysEvaluate, swapCommands;

            public ConditionToken(IVariable value, bool alwaysEval, bool swap) : base(value) {
                alwaysEvaluate = alwaysEval;
                swapCommands = swap;
            }
        }

        public class CommandToken : ValueToken<Command> {
            public CommandToken(Command value) : base(value) { }
        }


        public class BlockConditionToken : ValueToken<BlockCondition> {
            public BlockConditionToken(BlockCondition value) : base(value) { }
        }

        public class RepetitionToken : ValueToken<IVariable> {
            public RepetitionToken(IVariable value) : base(value) { }
        }

        public class AggregationModeToken : ValueToken<AggregationMode> {
            public AggregationModeToken(AggregationMode value) : base(value) {
            }
        }

        public class PropertyAggregationToken : ValueToken<Aggregator> {
            public PropertyAggregationToken(Aggregator value) : base(value) {
            }
        }

        public class SelectorToken : ValueToken<ISelector> {
            public SelectorToken(ISelector value) : base(value) {
            }
        }

        public class AmbiguousSelectorToken : SelectorToken {
            public AmbiguousSelectorToken(BlockSelector value) : base(value) {
            }
        }

        public class BlockGroupToken : ValueToken<Block> {
            public bool group;
            public BlockGroupToken(Block value, bool g) : base(value) {
                group = g;
            }
        }
        #endregion
    }
}
