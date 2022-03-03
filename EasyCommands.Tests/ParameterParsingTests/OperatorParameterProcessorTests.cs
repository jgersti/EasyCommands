using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using Malware.MDKUtilities;
using IngameScript;
using static IngameScript.Program;
using static EasyCommands.Tests.ParameterParsingTests.ParsingTestUtility;

namespace EasyCommands.Tests.ParameterParsingTests {
    [TestClass]
    public class OperatorParameterProcessorTests : ForceLocale {
        [TestMethod]
        public void AssignAbsoluteValue() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to abs -3 + 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.ADD, variable.operation);
            Assert.AreEqual(5f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignSin() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to sin 1.5708");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            UnaryOperationVariable variable = (UnaryOperationVariable)assignment.variable;
            Assert.AreEqual(UnaryOperator.SIN, variable.operation);
            Assert.AreEqual(1f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignCos() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to cos 1.5708");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            UnaryOperationVariable variable = (UnaryOperationVariable)assignment.variable;
            Assert.AreEqual(UnaryOperator.COS, variable.operation);
            Assert.AreEqual((float)Math.Cos(1.5708f), variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignTan() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to tan 1.5708");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            UnaryOperationVariable variable = (UnaryOperationVariable)assignment.variable;
            Assert.AreEqual(UnaryOperator.TAN, variable.operation);
            Assert.AreEqual((float)Math.Tan(1.5708f), variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignASin() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to asin 1.5708");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            UnaryOperationVariable variable = (UnaryOperationVariable)assignment.variable;
            Assert.AreEqual(UnaryOperator.ASIN, variable.operation);
            Assert.AreEqual((float)Math.Asin(1.5708f), variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignACos() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to acos 1.5708");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            UnaryOperationVariable variable = (UnaryOperationVariable)assignment.variable;
            Assert.AreEqual(UnaryOperator.ACOS, variable.operation);
            Assert.AreEqual((float)Math.Acos(1.5708f), variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignATan() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to atan 1.5708");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            UnaryOperationVariable variable = (UnaryOperationVariable)assignment.variable;
            Assert.AreEqual(UnaryOperator.ATAN, variable.operation);
            Assert.AreEqual((float)Math.Atan(1.5708f), variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignNaturalLog() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to ln 1.5");
            Assert.IsTrue(command is VariableAssignmentCommand);
            var assignment = command as VariableAssignmentCommand;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            var variable = assignment.variable as UnaryOperationVariable;
            Assert.AreEqual(UnaryOperator.LN, variable.operation);
            Assert.AreEqual((float)Math.Log(1.5f), variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignSign() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to sign -0.5");
            Assert.IsTrue(command is VariableAssignmentCommand);
            var assignment = command as VariableAssignmentCommand;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            var variable = assignment.variable as UnaryOperationVariable;
            Assert.AreEqual(UnaryOperator.SIGN, variable.operation);
            Assert.AreEqual(-1f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignRoundDown() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to round 5.4");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            UnaryOperationVariable variable = (UnaryOperationVariable)assignment.variable;
            Assert.AreEqual(UnaryOperator.ROUND, variable.operation);
            Assert.AreEqual(5f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignRoundUp() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to round 5.6");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            UnaryOperationVariable variable = (UnaryOperationVariable)assignment.variable;
            Assert.AreEqual(UnaryOperator.ROUND, variable.operation);
            Assert.AreEqual(6f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignRoundVector() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to round (5.6:0:5.4)");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            UnaryOperationVariable variable = (UnaryOperationVariable)assignment.variable;
            Assert.AreEqual(UnaryOperator.ROUND, variable.operation);
            Assert.AreEqual(new Vector3D(6f, 0f, 5f), variable.GetValue().AsVector());
        }

        [TestMethod]
        public void AssignAbsoluteValueVector() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to abs 1:0:0 + 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.ADD, variable.operation);
            Assert.IsTrue(variable.a is UnaryOperationVariable);
            UnaryOperationVariable operation = (UnaryOperationVariable)variable.a;
            Assert.AreEqual(UnaryOperator.ABS, operation.operation);
            Assert.AreEqual(Return.VECTOR, operation.a.GetValue().returnType);
            Assert.AreEqual(Return.NUMERIC, variable.a.GetValue().returnType);
            Assert.AreEqual(3f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignSquareRootValue() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to sqrt 9 + 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.ADD, variable.operation);
            Assert.AreEqual(5f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignSquareRootValueVector() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to sqrt 9:0:0 + 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.ADD, variable.operation);
            Assert.IsTrue(variable.a is UnaryOperationVariable);
            UnaryOperationVariable operation = (UnaryOperationVariable)variable.a;
            Assert.AreEqual(UnaryOperator.SQRT, operation.operation);
            Assert.AreEqual(Return.VECTOR, operation.a.GetValue().returnType);
            Assert.AreEqual(Return.NUMERIC, variable.a.GetValue().returnType);
            Assert.AreEqual(5f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignSimpleAddition() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 3 + 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.ADD, variable.operation);
            Assert.AreEqual(5f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignSimpleSubtraction() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 3 - 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.SUBTRACT, variable.operation);
            Assert.AreEqual(1f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignSimpleStringSubtraction() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to \"test\" - t");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.SUBTRACT, variable.operation);
            Assert.AreEqual("est", variable.GetValue().AsString());
        }

        [TestMethod]
        public void AssignNegativeVariable() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to -t");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            UnaryOperationVariable variable = (UnaryOperationVariable)assignment.variable;
            Assert.AreEqual(UnaryOperator.REVERSE, variable.operation);
            Assert.IsTrue(variable.a is AmbiguousStringVariable);
            AmbiguousStringVariable memoryVariable = (AmbiguousStringVariable)variable.a;
            Assert.AreEqual("t", memoryVariable.value);
        }

        [TestMethod]
        public void AssignSimpleStringSubtractionMultipleCharacters() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to \"second\" - eco");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.SUBTRACT, variable.operation);
            Assert.AreEqual("snd", variable.GetValue().AsString());
        }

        [TestMethod]
        public void AssignSimpleStringSubtractionEmptyString() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to \"second\" - \"\"");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.SUBTRACT, variable.operation);
            Assert.AreEqual("second", variable.GetValue().AsString());
        }

        [TestMethod]
        public void AssignSimpleStringSubtractionLastCharacter() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to \"second\" - d");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.SUBTRACT, variable.operation);
            Assert.AreEqual("secon", variable.GetValue().AsString());
        }

        [TestMethod]
        public void AssignSimpleStringSubtractionDoesNotContain() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to \"second\" - f");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.SUBTRACT, variable.operation);
            Assert.AreEqual("second", variable.GetValue().AsString());
        }

        [TestMethod]
        public void AssignSimpleStringSubtractionSubString() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to \"second\" - 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.SUBTRACT, variable.operation);
            Assert.AreEqual("seco", variable.GetValue().AsString());
        }

        [TestMethod]
        public void AssignSimpleStringSubtractionSubStringMoreThanLength() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to \"second\" - 6");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.SUBTRACT, variable.operation);
            Assert.AreEqual("", variable.GetValue().AsString());
        }

        [TestMethod]
        public void AssignSimpleMultiplication() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 3 * 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.MULTIPLY, variable.operation);
            Assert.AreEqual(6f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignVectorNumericAddition() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 1:0:0 + 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.ADD, variable.operation);
            Assert.AreEqual(new Vector3D(3, 0, 0), variable.GetValue().AsVector());
        }

        [TestMethod]
        public void AssignVectorNumericSubtraction() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 1:0:0 - 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.SUBTRACT, variable.operation);
            Assert.AreEqual(new Vector3D(-1, 0, 0), variable.GetValue().AsVector());
        }

        [TestMethod]
        public void AssignVectorNumericMultiplication() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 1:0:0 * 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.MULTIPLY, variable.operation);
            Assert.AreEqual(new Vector3D(2, 0, 0), variable.GetValue().AsVector());
        }

        [TestMethod]
        public void AssignVectorNumericDivision() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 2:0:0 / 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.DIVIDE, variable.operation);
            Assert.AreEqual(new Vector3D(1, 0, 0), variable.GetValue().AsVector());
        }

        [TestMethod]
        public void AssignVectorMultiplication() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 0:1:0 * 1:0:0");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.MULTIPLY, variable.operation);
            Assert.AreEqual(new Vector3D(0,0,-1), variable.GetValue().AsVector());
        }

        [TestMethod]
        public void AssignVectorDotProduct() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 0:1:0 . 1:0:0");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.DOT, variable.operation);
            Assert.AreEqual(0f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignVectorSign() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to sign -0.5:1.5:0");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is UnaryOperationVariable);
            var variable = assignment.variable as UnaryOperationVariable;
            Assert.AreEqual(UnaryOperator.SIGN, variable.operation);
            Assert.AreEqual(new Vector3D(-1,1,0), variable.GetValue().AsVector());
        }

        [TestMethod]
        public void AssignSimpleDivision() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 6 / 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.DIVIDE, variable.operation);
            Assert.AreEqual(3f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignSimpleMod() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 5 % 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.MOD, variable.operation);
            Assert.AreEqual(1f, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignStringMod() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to test % t");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.MOD, variable.operation);
            Assert.AreEqual("es", variable.GetValue().AsString());
        }

        [TestMethod]
        public void AssignVectorMod() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 1:1:0 % 0:1:0");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.MOD, variable.operation);
            Assert.AreEqual(new Vector3D(1,0,0), variable.GetValue().AsVector());
        }

        [TestMethod]
        public void AssignSimpleExponent() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 2 ^ 4");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.EXPONENT, variable.operation);
            Assert.AreEqual(16, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignBoolExponentAsXOR() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to true ^ false");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.EXPONENT, variable.operation);
            Assert.IsTrue(variable.GetValue().AsBool());
        }

        [TestMethod]
        public void AssignBoolExponentAsXORWhenFalse() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to true xor true");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.EXPONENT, variable.operation);
            Assert.IsFalse(variable.GetValue().AsBool());
        }

        [TestMethod]
        public void AssignVectorExponentAsAngleBetween() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 0:0:1 ^ 1:0:0");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.EXPONENT, variable.operation);
            Assert.AreEqual(90, variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignSimpleAdditionVariable() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to b + 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.ADD, variable.operation);
            Assert.IsTrue(variable.a is AmbiguousStringVariable);
            Assert.IsTrue(variable.b is StaticVariable);
        }

        [TestMethod]
        public void AssignSimpleSubtractionVariable() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to b - 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.SUBTRACT, variable.operation);
            Assert.IsTrue(variable.a is AmbiguousStringVariable);
            Assert.IsTrue(variable.b is StaticVariable);
        }

        [TestMethod]
        public void AssignSimpleMultiplicationVariable() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to b * 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.MULTIPLY, variable.operation);
            Assert.IsTrue(variable.a is AmbiguousStringVariable);
            Assert.IsTrue(variable.b is StaticVariable);
        }

        [TestMethod]
        public void AssignSimpleDivisionVariable() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to b / 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.DIVIDE, variable.operation);
            Assert.IsTrue(variable.a is AmbiguousStringVariable);
            Assert.IsTrue(variable.b is StaticVariable);
        }

        [TestMethod]
        public void MultiplicationBeforeAddition() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 4 * 2 + 3");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable addVariable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(11f, addVariable.GetValue().AsNumber());
            Assert.AreEqual(BinaryOperator.ADD, addVariable.operation);
            Assert.IsTrue(addVariable.a is BinaryOperationVariable);
            Assert.IsTrue(addVariable.b is StaticVariable);
            BinaryOperationVariable multiplyVariable = (BinaryOperationVariable)addVariable.a;
            Assert.AreEqual(BinaryOperator.MULTIPLY, multiplyVariable.operation);
        }

        [TestMethod]
        public void DivisionBeforeAddition() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 4 / 2 + 3");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable addVariable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(5f, addVariable.GetValue().AsNumber());
            Assert.AreEqual(BinaryOperator.ADD, addVariable.operation);
            Assert.IsTrue(addVariable.a is BinaryOperationVariable);
            Assert.IsTrue(addVariable.b is StaticVariable);
            BinaryOperationVariable multiplyVariable = (BinaryOperationVariable)addVariable.a;
            Assert.AreEqual(BinaryOperator.DIVIDE, multiplyVariable.operation);
        }

        [TestMethod]
        public void AdditionBeforeVariableComparison() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to b + 1 > 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is ComparisonVariable);
            ComparisonVariable variable = (ComparisonVariable)assignment.variable;
            Assert.IsTrue(variable.a is BinaryOperationVariable);
            Assert.IsTrue(variable.b is StaticVariable);
        }

        [TestMethod]
        public void AdditionBeforeBooleanLogic() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to b + 1 and c");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.AND, variable.operation);
            Assert.IsTrue(variable.a is BinaryOperationVariable);
            Assert.IsTrue(variable.b is AmbiguousStringVariable);
        }

        [TestMethod]
        public void TernaryOperator() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign e to a > b ? c : d + 1");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is TernaryConditionVariable);
            TernaryConditionVariable variable = (TernaryConditionVariable)assignment.variable;
            Assert.IsTrue(variable.condition is ComparisonVariable);
            ComparisonVariable condition = (ComparisonVariable)variable.condition;
            Assert.IsTrue(condition.a is AmbiguousStringVariable);
            Assert.IsTrue(condition.b is AmbiguousStringVariable);
            Assert.AreEqual("a", ((AmbiguousStringVariable)condition.a).value);
            Assert.AreEqual("b", ((AmbiguousStringVariable)condition.b).value);
            Assert.IsTrue(variable.positiveValue is AmbiguousStringVariable);
            Assert.AreEqual("c", ((AmbiguousStringVariable)variable.positiveValue).value);
            Assert.IsTrue(variable.negativeValue is BinaryOperationVariable);
        }

        [TestMethod]
        public void ComparisonBeforeBooleanLogic() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign c to a > 0 and b < 1");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignment.variable is BinaryOperationVariable);
            BinaryOperationVariable variable = (BinaryOperationVariable)assignment.variable;
            Assert.AreEqual(BinaryOperator.AND, variable.operation);
            Assert.IsTrue(variable.a is ComparisonVariable);
            Assert.IsTrue(variable.b is ComparisonVariable);
            ComparisonVariable a = (ComparisonVariable)variable.a;
            Assert.IsTrue(a.a is AmbiguousStringVariable);
            Assert.IsTrue(a.b is StaticVariable);
            ComparisonVariable b = (ComparisonVariable)variable.b;
            Assert.IsTrue(b.a is AmbiguousStringVariable);
            Assert.IsTrue(b.b is StaticVariable);
        }

        [TestMethod]
        public void AdditionUsedAsBlockConditionVariable() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("if the \"rotor\" angle > a + 30 set the \"rotor\" angle to a");
            Assert.IsTrue(command is ConditionalCommand);
            ConditionalCommand conditionalCommand = (ConditionalCommand)command;
            Assert.IsTrue(conditionalCommand.condition is AggregateConditionVariable);
            AggregateConditionVariable condition = (AggregateConditionVariable)conditionalCommand.condition;
            PropertySupplier property = GetDelegateProperty<PropertySupplier>("property", condition.blockCondition);
            Assert.AreEqual(Property.ANGLE + "", property.propertyType);
            BinaryOperationVariable comparisonValue = GetDelegateProperty<BinaryOperationVariable>("comparisonValue", condition.blockCondition);
            Assert.IsTrue(comparisonValue.a is AmbiguousStringVariable);
            Assert.IsTrue(comparisonValue.b is StaticVariable);
            Assert.AreEqual("a", comparisonValue.a.GetValue().value);
            Assert.AreEqual(30f, comparisonValue.b.GetValue().value);
        }

        [TestMethod]
        public void AssignColor() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to red");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive primitive = assignment.variable.GetValue();
            Assert.AreEqual(Return.COLOR, primitive.returnType);
            Assert.AreEqual(Color.Red, primitive.value);
        }

        [TestMethod]
        public void AssignColorFromHex() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to #ff0000");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive primitive = assignment.variable.GetValue();
            Assert.AreEqual(Return.COLOR, primitive.returnType);
            Assert.AreEqual(Color.Red, primitive.value);
        }

        [TestMethod]
        public void AssignAddedColors() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to #ff0000 + #00ff00");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive primitive = assignment.variable.GetValue();
            Assert.AreEqual(Return.COLOR, primitive.returnType);
            Assert.AreEqual(Color.Yellow, primitive.value);
        }

        [TestMethod]
        public void AssignSubtractedColors() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to #ffff00 - #00ff00");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive primitive = assignment.variable.GetValue();
            Assert.AreEqual(Return.COLOR, primitive.returnType);
            Assert.AreEqual(Color.Red, primitive.value);
        }

        [TestMethod]
        public void AssignMultipliedColor() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to #112233 * 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive primitive = assignment.variable.GetValue();
            Assert.AreEqual(Return.COLOR, primitive.returnType);
            Assert.AreEqual("#224466", primitive.AsString());
        }

        [TestMethod]
        public void AssignDividedColor() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to #224466 / 2");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive primitive = assignment.variable.GetValue();
            Assert.AreEqual(Return.COLOR, primitive.returnType);
            Assert.AreEqual("#112233", primitive.AsString());
        }

        [TestMethod]
        public void AssignNotColor() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to not red");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive primitive = assignment.variable.GetValue();
            Assert.AreEqual(Return.COLOR, primitive.returnType);
            Assert.AreEqual(Color.Cyan, primitive.value);
        }

        [TestMethod]
        public void AddNumberToList() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to [0, 1, 2] + 3");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            List<IVariable> listValues = assignment.variable.GetValue().AsList().GetValues();
            Assert.AreEqual(4, listValues.Count);
            Assert.AreEqual(0f, listValues[0].GetValue().value);
            Assert.AreEqual(1f, listValues[1].GetValue().value);
            Assert.AreEqual(2f, listValues[2].GetValue().value);
            Assert.AreEqual(3f, listValues[3].GetValue().value);
        }

        [TestMethod]
        public void AddNumberToListInFront() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to 0 + [1, 2, 3]");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            List<IVariable> listValues = assignment.variable.GetValue().AsList().GetValues();
            Assert.AreEqual(4, listValues.Count);
            Assert.AreEqual(0f, listValues[0].GetValue().value);
            Assert.AreEqual(1f, listValues[1].GetValue().value);
            Assert.AreEqual(2f, listValues[2].GetValue().value);
            Assert.AreEqual(3f, listValues[3].GetValue().value);
        }

        [TestMethod]
        public void AddStringToList() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to [0, 1, 2] + \" three\"");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.AreEqual("[0,1,2] three", assignment.variable.GetValue().AsString());
        }

        [TestMethod]
        public void AddStringToListInFront() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to \"zero \" + [0, 1, 2]");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Assert.AreEqual("zero [0,1,2]", assignment.variable.GetValue().AsString());
        }

        [TestMethod]
        public void AddTwoLists() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to [0, 1, 2] + [3, 4, 5]");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            List<IVariable> listValues = assignment.variable.GetValue().AsList().GetValues();
            Assert.AreEqual(6, listValues.Count);
            Assert.AreEqual(0f, listValues[0].GetValue().value);
            Assert.AreEqual(1f, listValues[1].GetValue().value);
            Assert.AreEqual(2f, listValues[2].GetValue().value);
            Assert.AreEqual(3f, listValues[3].GetValue().value);
            Assert.AreEqual(4f, listValues[4].GetValue().value);
            Assert.AreEqual(5f, listValues[5].GetValue().value);
        }

        [TestMethod]
        public void CastStringAsVector() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to (\"1\" + \":2:\" + \"3\") as \"vector\"");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive value = assignment.variable.GetValue();
            Assert.AreEqual(Return.VECTOR, value.returnType);
            Assert.AreEqual("1:2:3", value.AsString());
        }

        [TestMethod]
        public void CastStringAsBoolean() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to \"true\" as \"bool\"");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive value = assignment.variable.GetValue();
            Assert.AreEqual(Return.BOOLEAN, value.returnType);
            Assert.AreEqual(true, value.value);
        }

        [TestMethod]
        public void SplitStringBySubString() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to \"My Value\" split \" \"");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive value = assignment.variable.GetValue();
            Assert.AreEqual(Return.LIST, value.returnType);
            Assert.AreEqual("[My,Value]", value.AsString());
        }

        [TestMethod]
        public void SplitStringByNewLine() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to \"My\nValue\" split \"\\n\"");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive value = assignment.variable.GetValue();
            Assert.AreEqual(Return.LIST, value.returnType);
            Assert.AreEqual("[My,Value]", value.AsString());
        }

        [TestMethod]
        public void JoinListByString() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to [1,2,3] joined \", \"");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive value = assignment.variable.GetValue();
            Assert.AreEqual(Return.STRING, value.returnType);
            Assert.AreEqual("1, 2, 3", value.AsString());
        }

        [TestMethod]
        public void JoinListByNewLine() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign a to [1,2,3] joined \"\\n\"");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignment = (VariableAssignmentCommand)command;
            Primitive value = assignment.variable.GetValue();
            Assert.AreEqual(Return.STRING, value.returnType);
            Assert.AreEqual("1\n2\n3", value.value);
        }
    }
}
