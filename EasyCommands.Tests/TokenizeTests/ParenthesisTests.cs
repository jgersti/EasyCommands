using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Malware.MDKUtilities;
using IngameScript;
using static IngameScript.Program;

namespace EasyCommands.Tests.TokenizeTests {
    [TestClass]
    public class ParenthesisTests : ForceLocale {
        [TestMethod]
        public void TestBasicParenthesis() {
            var tokens = Lexer.Tokenize("test ( string )").ToList();
            Assert.AreEqual(4, tokens.Count);
            Assert.AreEqual("test", tokens[0].original);
            Assert.AreEqual("(", tokens[1].original);
            Assert.AreEqual("string", tokens[2].original);
            Assert.AreEqual(")", tokens[3].original);
        }

        [TestMethod]
        public void TestParenthesesMissingSpaces() {
            var tokens = Lexer.Tokenize("test (string) there").ToList();
            Assert.AreEqual(5, tokens.Count);
            Assert.AreEqual("test", tokens[0].original);
            Assert.AreEqual("(", tokens[1].original);
            Assert.AreEqual("string", tokens[2].original);
            Assert.AreEqual(")", tokens[3].original);
            Assert.AreEqual("there", tokens[4].original);
        }

        [TestMethod]
        public void TestMissingSpaceBeforeOpeningParenthesis() {
            var tokens = Lexer.Tokenize("test (string )there").ToList();
            Assert.AreEqual(5, tokens.Count);
            Assert.AreEqual("test", tokens[0].original);
            Assert.AreEqual("(", tokens[1].original);
            Assert.AreEqual("string", tokens[2].original);
            Assert.AreEqual(")", tokens[3].original);
            Assert.AreEqual("there", tokens[4].original);
        }

        [TestMethod]
        public void TestMissingSpaceAfterClosingParenthesis() {
            var tokens = Lexer.Tokenize("test( string ) there").ToList();
            Assert.AreEqual(5, tokens.Count);
            Assert.AreEqual("test", tokens[0].original);
            Assert.AreEqual("(", tokens[1].original);
            Assert.AreEqual("string", tokens[2].original);
            Assert.AreEqual(")", tokens[3].original);
            Assert.AreEqual("there", tokens[4].original);
        }

        [TestMethod]
        public void TestEmbeddedParenthesesMissingSpaces() {
            var tokens = Lexer.Tokenize("test ((string) there)").ToList();
            Assert.AreEqual(7, tokens.Count);
            Assert.AreEqual("test", tokens[0].original);
            Assert.AreEqual("(", tokens[1].original);
            Assert.AreEqual("(", tokens[2].original);
            Assert.AreEqual("string", tokens[3].original);
            Assert.AreEqual(")", tokens[4].original);
            Assert.AreEqual("there", tokens[5].original);
            Assert.AreEqual(")", tokens[6].original);
        }
    }
}
