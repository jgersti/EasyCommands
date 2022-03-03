using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Malware.MDKUtilities;
using IngameScript;
using static IngameScript.Program;

namespace EasyCommands.Tests.ParameterParsingTests {
    [TestClass]
    public class ListParameterProcessorTests : ForceLocale {
        [TestMethod]
        public void AssignVariableToBasicStaticList() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign myList to [\"one\", \"two\", \"three\"]");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignmentCommand = (VariableAssignmentCommand)command;
            Assert.AreEqual("myList", assignmentCommand.variableName);
            Assert.IsTrue(assignmentCommand.variable is ListIndexVariable);
            ListIndexVariable listIndex = (ListIndexVariable)assignmentCommand.variable;
            Primitive list = listIndex.expectedList.GetValue();
            Assert.AreEqual(Return.LIST, list.returnType);
            List<IVariable> listItems = list.AsList().GetValues();
            Assert.AreEqual(3, listItems.Count);
            Assert.AreEqual("one", listItems[0].GetValue().AsString());
            Assert.AreEqual("two", listItems[1].GetValue().AsString());
            Assert.AreEqual("three", listItems[2].GetValue().AsString());
        }

        [TestMethod]
        public void AssignVariableToBasicStaticListWithBlockType() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign myList to [\"reactor component\"]");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignmentCommand = (VariableAssignmentCommand)command;
            Assert.AreEqual("myList", assignmentCommand.variableName);
            Assert.IsTrue(assignmentCommand.variable is ListIndexVariable);
            ListIndexVariable listIndex = (ListIndexVariable)assignmentCommand.variable;
            Primitive list = listIndex.expectedList.GetValue();
            Assert.AreEqual(Return.LIST, list.returnType);
            List<IVariable> listItems = list.AsList().GetValues();
            Assert.AreEqual(1, listItems.Count);
            Assert.AreEqual("reactor component", listItems[0].GetValue().AsString());
        }

        [TestMethod]
        public void AssignVariableIndexToVariable() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign myList[0] to \"value\"");
            Assert.IsTrue(command is ListVariableAssignmentCommand);
            ListVariableAssignmentCommand assignmentCommand = (ListVariableAssignmentCommand)command;
            Assert.IsTrue(assignmentCommand.list.expectedList is AmbiguousStringVariable);
            AmbiguousStringVariable listName = (AmbiguousStringVariable)assignmentCommand.list.expectedList;
            Assert.AreEqual("myList", listName.value);
            List<IVariable> listIndexes = assignmentCommand.list.index.GetValue().AsList().GetValues();
            Assert.AreEqual(1, listIndexes.Count);
            Assert.AreEqual(0, listIndexes[0].GetValue().AsNumber());
            Primitive value = assignmentCommand.value.GetValue();
            Assert.AreEqual("value", value.AsString());
        }

        [TestMethod]
        public void AssignVariableIndexToStaticList() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign myList[0] to [\"one\", \"two\", \"three\"]");
            Assert.IsTrue(command is ListVariableAssignmentCommand);
            ListVariableAssignmentCommand assignmentCommand = (ListVariableAssignmentCommand)command;
            Assert.IsTrue(assignmentCommand.list.expectedList is AmbiguousStringVariable);
            AmbiguousStringVariable listName = (AmbiguousStringVariable)assignmentCommand.list.expectedList;
            Assert.AreEqual("myList", listName.value);
            List<IVariable> listIndexes = assignmentCommand.list.index.GetValue().AsList().GetValues();
            Assert.AreEqual(1, listIndexes.Count);
            Assert.AreEqual(0, listIndexes[0].GetValue().AsNumber());
            Primitive list = assignmentCommand.value.GetValue();
            Assert.AreEqual(Return.LIST, list.returnType);
            List<IVariable> listItems = list.AsList().GetValues();
            Assert.AreEqual(3, listItems.Count);
            Assert.AreEqual("one", listItems[0].GetValue().AsString());
            Assert.AreEqual("two", listItems[1].GetValue().AsString());
            Assert.AreEqual("three", listItems[2].GetValue().AsString());
        }

        [TestMethod]
        public void AssignMultipleVariableIndexesToStaticValue() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign myList[(0 .. 2) + [4]] to \"value\"");
            Assert.IsTrue(command is ListVariableAssignmentCommand);
            ListVariableAssignmentCommand assignmentCommand = (ListVariableAssignmentCommand)command;
            Assert.IsTrue(assignmentCommand.list.expectedList is AmbiguousStringVariable);
            AmbiguousStringVariable listName = (AmbiguousStringVariable)assignmentCommand.list.expectedList;
            Assert.AreEqual("myList", listName.value);
            List<IVariable> listIndexes = assignmentCommand.list.index.GetValue().AsList().GetValues();
            Assert.AreEqual(4, listIndexes.Count);
            Assert.AreEqual(0, listIndexes[0].GetValue().AsNumber());
            Assert.AreEqual(1, listIndexes[1].GetValue().AsNumber());
            Assert.AreEqual(2, listIndexes[2].GetValue().AsNumber());
            Assert.AreEqual(4, listIndexes[3].GetValue().AsNumber());
            Assert.AreEqual("value", assignmentCommand.value.GetValue().AsString());
        }

        [TestMethod]
        public void AssignVariableToListIndexValue() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign myValue to [1, 2, 3][0]");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignmentCommand = (VariableAssignmentCommand)command;
            Assert.AreEqual("myValue", assignmentCommand.variableName);
            Primitive value = assignmentCommand.variable.GetValue();
            Assert.AreEqual(1f, value.AsNumber());
        }

        [TestMethod]
        public void AssignVariableToListSubRange() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign myValue to (3..10)[2..4]");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignmentCommand = (VariableAssignmentCommand)command;
            Assert.AreEqual("myValue", assignmentCommand.variableName);
            List<IVariable> values = assignmentCommand.variable.GetValue().AsList().GetValues();
            Assert.AreEqual(3, values.Count);
            Assert.AreEqual(5f, values[0].GetValue().AsNumber());
            Assert.AreEqual(6f, values[1].GetValue().AsNumber());
            Assert.AreEqual(7f, values[2].GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignListAtIndexValuesToAnotherList() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign myList[1, 2, 3] to [0]");
            Assert.IsTrue(command is ListVariableAssignmentCommand);
            ListVariableAssignmentCommand assignmentCommand = (ListVariableAssignmentCommand)command;
            Assert.IsTrue(assignmentCommand.list.expectedList is AmbiguousStringVariable);
            AmbiguousStringVariable listName = (AmbiguousStringVariable)assignmentCommand.list.expectedList;
            Assert.AreEqual("myList", listName.value);
            List<IVariable> listIndexes = assignmentCommand.list.index.GetValue().AsList().GetValues();
            Assert.AreEqual(3, listIndexes.Count);
            Assert.AreEqual(1, listIndexes[0].GetValue().AsNumber());
            Assert.AreEqual(2, listIndexes[1].GetValue().AsNumber());
            Assert.AreEqual(3, listIndexes[2].GetValue().AsNumber());
            List<IVariable> assignedValue = assignmentCommand.value.GetValue().AsList().GetValues();
            Assert.AreEqual(1, assignedValue.Count);
            Assert.AreEqual(0f, assignedValue[0].GetValue().value);
        }

        [TestMethod]
        public void AssignVariableToMultiDimensionalArray() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign myList to [ [0,1,2] , [3,4,5] ]");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignmentCommand = (VariableAssignmentCommand)command;
            Assert.AreEqual("myList", assignmentCommand.variableName);
            List<IVariable> values = assignmentCommand.variable.GetValue().AsList().GetValues();
            Assert.AreEqual(2, values.Count);

            List<IVariable> myList0 = values[0].GetValue().AsList().GetValues();
            Assert.AreEqual(0f, myList0[0].GetValue().AsNumber());
            Assert.AreEqual(1f, myList0[1].GetValue().AsNumber());
            Assert.AreEqual(2f, myList0[2].GetValue().AsNumber());

            List<IVariable> myList1 = values[1].GetValue().AsList().GetValues();
            Assert.AreEqual(3f, myList1[0].GetValue().AsNumber());
            Assert.AreEqual(4f, myList1[1].GetValue().AsNumber());
            Assert.AreEqual(5f, myList1[2].GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignVariableToMultiDimensionalArrayValue() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign myValue to [ [0,1,2] , [3,4,5] ][1][2]");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignmentCommand = (VariableAssignmentCommand)command;
            Assert.AreEqual("myValue", assignmentCommand.variableName);
            Assert.AreEqual(5f, assignmentCommand.variable.GetValue().AsNumber());
        }

        [TestMethod]
        public void AssignVariableToMultiDimensionalArraySubList() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("assign myValue to [ [0,1,2] , [3,4,5] ][1]");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignmentCommand = (VariableAssignmentCommand)command;
            Assert.AreEqual("myValue", assignmentCommand.variableName);
            List<IVariable> listValues = assignmentCommand.variable.GetValue().AsList().GetValues();
            Assert.AreEqual(3, listValues.Count);
            Assert.AreEqual(3f, listValues[0].GetValue().value);
            Assert.AreEqual(4f, listValues[1].GetValue().value);
            Assert.AreEqual(5f, listValues[2].GetValue().value);
        }

        [TestMethod]
        public void CountOfListAsCondition() {
            var program = MDKFactory.CreateProgram<Program>();
            var command = program.ParseCommand("if count of shipRoute[] > 0 wait");
            Assert.IsTrue(command is ConditionalCommand);
            ConditionalCommand conditionalCommand = (ConditionalCommand)command;
            Assert.IsTrue(conditionalCommand.condition is ComparisonVariable);
            ComparisonVariable comparison = (ComparisonVariable)conditionalCommand.condition;
            Assert.IsTrue(comparison.a is ListAggregateVariable);
            ListAggregateVariable listAggregate = (ListAggregateVariable)comparison.a;
            Assert.IsTrue(listAggregate.expectedList is ListIndexVariable);
            ListIndexVariable listIndex = (ListIndexVariable)listAggregate.expectedList;
            Assert.IsTrue(listIndex.expectedList is AmbiguousStringVariable);
            AmbiguousStringVariable listVariable = (AmbiguousStringVariable)listIndex.expectedList;
            Assert.AreEqual("shipRoute", listVariable.value);
            Assert.IsTrue(comparison.b is StaticVariable);
            StaticVariable comparisonVariable = (StaticVariable)comparison.b;
            Assert.AreEqual(0f, comparisonVariable.GetValue().value);
        }
    }
}
