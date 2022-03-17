using EasyCommands.Utilities.Pika.Grammar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCommands.Utilities.Pika.Utils {
    public static class ClauseFactory {
        public static Rule Rule(string name, Clause clause) => Rule(name, -1, null, clause);
        public static Rule Rule(string name, int prec, Associativity? assoc, Clause clause)
            => new Rule(name, prec, assoc, clause);


        public static Clause Start() => new Start();
        public static Clause Nothing() => new Nothing();

        public static Clause Seq(params Clause[] clauses) => new Seq(clauses);
        public static Clause First(params Clause[] clauses) => new First(clauses);

        public static Clause OneOrMore(Clause clause)
            => (clause is OneOrMore || clause is Nothing || clause is FollowedBy || clause is NotFollowedBy|| clause is Start)
                    ? clause
                    : new OneOrMore(clause);

        public static Clause FollowedBy(Clause clause) {
            if (clause is Nothing)
                return clause;
            else if (clause is FollowedBy || clause is NotFollowedBy || clause is Start)
                throw new ArgumentException($"{typeof(FollowedBy).Name}({clause.GetType().Name}) is nonsensical");
            return new FollowedBy(clause);
        }

        public static Clause NotFollowedBy(Clause clause) {
            if (clause is Nothing)
                throw new ArgumentException($"{typeof(NotFollowedBy).Name}({typeof(Nothing).Name}) is nonsensical");
            else if (clause is NotFollowedBy)
                return new FollowedBy(clause.SubClauses.First().Clause);
            else if (clause is FollowedBy || clause is Start)
                throw new ArgumentException($"{typeof(NotFollowedBy).Name}({clause.GetType().Name}) is nonsensical");
            return new NotFollowedBy(clause);
        }

        public static Clause Optional(Clause clause) => First(clause, Nothing());
        public static Clause ZeroOrMore(Clause clause) => Optional(OneOrMore(clause));

        public static Clause Str(string str) => str.Length == 1 ? (Clause)Char(str[0]) : new CharSeq(str, false);

        public static CharSet Char(params char[] chars) => new CharSet(chars);
        public static CharSet CharInStr(string str) => new CharSet(str.ToArray());

        public static CharSet CharRange(char minChar, char maxChar) {
            if (maxChar < minChar) {
                throw new ArgumentException("maxChar < minChar");
            }
            BitArray bits = new BitArray(maxChar + 1);
            Enumerable.Range(minChar, maxChar - minChar + 1).ForEach(i => bits.Set(i, true));
            return new CharSet(bits);
        }

        public static CharSet CharRange(string charRangeStr) {
            bool invert = charRangeStr.StartsWith("^");
            var charList = StringUtils.GetCharRangeChars(invert ? charRangeStr.Substring(1) : charRangeStr);
            var chars = new BitArray(0xffff);
            for (int i = 0; i < charList.Count(); i++) {
                var c = charList.ElementAt(i);
                if (c.Length == 2)
                    c = c.Substring(1);

                var c0 = c[0];
                if (i <= charList.Count() - 3 && charList.ElementAt(i + 1).Equals("-")) {
                    var cEnd = charList.ElementAt(i + 2);
                    if (cEnd.Length == 2)
                        cEnd = cEnd.Substring(1);

                    var cEnd0 = cEnd[0];
                    if (cEnd0 < c0)
                        throw new ArgumentException($"Char range limits out of order: {c0}, {cEnd0}");

                    Enumerable.Range(c0, cEnd0 - c0).ForEach(j => chars.Set(j, true));
                    i += 2;
                } else
                    chars.Set(c0, true);
            }
            return invert ? new CharSet(chars).Invert() : new CharSet(chars);
        }

        public static CharSet Char(params CharSet[] sets) => new CharSet(sets);

        public static Clause Ast(string label, Clause clause) => new ASTNodeLabel(label, clause);
        public static Clause RuleRef(string ruleName) => new RuleRef(ruleName);
    }
}
