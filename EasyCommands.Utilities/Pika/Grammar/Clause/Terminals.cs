using EasyCommands.Utilities.Pika.Memoization;
using EasyCommands.Utilities.Pika.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCommands.Utilities.Pika.Grammar {
    public abstract class Terminal : Clause {
        protected Terminal() : base() {}
    }

    public class Start : Terminal {
        public const string START_STR = "^";

        public override void DetermineWhetherCanMatchZeroChars() => CanMatchZeroChars = true;
        public override Match Match(Table table, Key key, string input) => key.Start == 0 ? new Match(key) : null;
        public override string ToString() => ToStringCached = ToStringCached ?? START_STR;
    }

    public class Nothing : Terminal {
        public const string NOTHING_STR = "()";

        public override void DetermineWhetherCanMatchZeroChars() => CanMatchZeroChars = true;
        public override Match Match(Table table, Key key, string input) => new Match(key);
        public override string ToString() => ToStringCached = ToStringCached ?? NOTHING_STR;
    }

    public class CharSet : Terminal {
        private BitArray chars;
        private BitArray invertedChars;

        public CharSet(params char[] chars) : base() {
            this.chars = new BitArray(0xffff);
            chars.ForEach(c => this.chars.Set(c, true));
        }

        public CharSet(params CharSet[] charSets) : base() {
            if (charSets.Length == 0)
                throw new ArgumentException("Must provide at least one CharSet");

            chars = new BitArray(0xffff);
            foreach (var set in charSets) {
                if (set.chars != null) {
                    if (set.chars.Count != chars.Count)
                        set.chars.Length = chars.Count;
                    chars.Or(set.chars);
                }
                if (set.invertedChars != null) {
                    if (invertedChars == null)
                        invertedChars = new BitArray(0xffff);
                    if (set.invertedChars.Count != invertedChars.Count)
                        set.invertedChars.Length = invertedChars.Count;
                    invertedChars.Or(set.invertedChars);
                }
            }
        }

        public CharSet(BitArray chars) : base() {
            if (!chars.Cast<bool>().Any(b => b))
                throw new ArgumentException("Must provide at least one char in a CharSet");
            this.chars = chars;
            this.chars.Length = 0xffff;
        }

        public CharSet Invert() {
            var tmp = chars;
            chars = invertedChars;
            invertedChars = tmp;
            ToStringCached = null;
            return this;
        }

        public override void DetermineWhetherCanMatchZeroChars() {}
        public override Match Match(Table table, Key key, string input) {
            if (key.Start < input.Length) {
                var c = input[key.Start];
                if ((chars?[c] ?? false) || (!invertedChars?[c] ?? false))
                    return new Match(key, 1); // , Memoization.Match.NO_SUBCLAUSE_MATCHES);
            }
            return null;
        }

        public override string ToString() {
            if (ToStringCached == null) {
                var buffer = new StringBuilder();
                var charsCardinality = chars?.Cast<bool>().Count(b => b) ?? 0;
                var invertedCardinality = invertedChars?.Cast<bool>().Count(b => b) ?? 0;
                var invertedAndNot = charsCardinality > 0 && invertedCardinality > 0;
                if (invertedAndNot)
                    buffer.Append('(');
                if (charsCardinality > 0)
                    ToString(chars, charsCardinality, false, buffer);
                if (invertedAndNot)
                    buffer.Append('|');
                if (invertedCardinality > 0)
                    ToString(invertedChars, invertedCardinality, true, buffer);
                if (invertedAndNot)
                    buffer.Append(')');
                ToStringCached = buffer.ToString();
            }
            return ToStringCached;
        }

        public static void  ToString(BitArray bits, int cardinality, bool inverted, StringBuilder buffer) {
            var chars = Enumerable.Range(0, bits.Count).Where(i => bits[i]).Select(i => (char)i).ToArray();
            bool isSingleChar = !inverted && cardinality == 1;
            if (isSingleChar) {
                buffer.Append('\'').Append(StringUtils.EscapeQuotedChar(chars.Single())).Append('\'');
            } else {
                buffer.Append('[');
                if (inverted)
                    buffer.Append('^');
                var diff = chars.Zip(chars.Skip(1), (x, y) => y - x).ToArray();
                buffer.Append(StringUtils.EscapeCharRangeChar(chars[0]));
                int i = 0;
                while(i < diff.Length) { // diff.Length == chars.Length-1
                    if (diff[i] != 1)
                        buffer.Append(StringUtils.EscapeCharRangeChar(chars[++i]));
                    else {
                        var j = i;
                        while (j < diff.Length && diff[j] == 1) j++;
                        if (j - i > 2)
                            buffer.Append('-');
                        buffer.Append(StringUtils.EscapeCharRangeChar(chars[j]));
                        i = j;
                    }
                }
                buffer.Append(']');
            }
        }
    }

    public class CharSeq : Terminal {
        public string String { get; }
        public bool IgnoreCase { get; }

        public CharSeq(string str, bool ignoreCase) : base() {
            String = str;
            IgnoreCase = ignoreCase;
        }

        public override void DetermineWhetherCanMatchZeroChars() { }
        public override Match Match(Table table, Key key, string input)
            => key.Start <= input.Length - String.Length && input.IndexOf(String, key.Start, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) == key.Start
                ? new Match(key, String.Length)
                : null;

        public override string ToString() => ToStringCached = ToStringCached ?? $"\"{StringUtils.EscapeString(String)}\"";
    }


}
