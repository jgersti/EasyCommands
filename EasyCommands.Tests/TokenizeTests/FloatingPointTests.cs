﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Malware.MDKUtilities;
using IngameScript;
using static IngameScript.Program;

namespace EasyCommands.Tests.TokenizeTests {
    [TestClass]
    public class FloatingPointTests : ForceLocale {
        [TestMethod]
        public void SeparateTokensMissingSpaces() {
            VerifyTokensSplit(",");
            VerifyTokensSplit("+");
            VerifyTokensSplit("*");
            VerifyTokensSplit("/");
            VerifyTokensSplit("!");
            VerifyTokensSplit("^");
            VerifyTokensSplit("..");
            //VerifyTokensSplit("."); that one would cause havok
            VerifyTokensSplit(":"); //might as well test this one now
            VerifyTokensSplit("%");
            VerifyTokensSplit(">");
            VerifyTokensSplit(">=");
            VerifyTokensSplit("<");
            VerifyTokensSplit("<=");
            VerifyTokensSplit("==");
            VerifyTokensSplit("=");
            VerifyTokensSplit("&&");
            VerifyTokensSplit("&");
            VerifyTokensSplit("||");
            VerifyTokensSplit("|");
            VerifyTokensSplit("@");
        }

        void VerifyTokensSplit(string token) {
            var tokens = Lexer.Tokenize("assign a to 2.0" + token + ".5").ToList();
            Assert.AreEqual(6, tokens.Count);
            Assert.AreEqual("assign", tokens[0].original);
            Assert.AreEqual("a", tokens[1].original);
            Assert.AreEqual("to", tokens[2].original);
            Assert.AreEqual("2.0", tokens[3].original);
            Assert.AreEqual(token, tokens[4].original);
            Assert.AreEqual(".5", tokens[5].original);
        }
    }
}
