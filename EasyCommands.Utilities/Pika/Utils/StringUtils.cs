using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCommands.Utilities.Pika.Utils {
    public static class StringUtils {
        const char NON_ASCII_CHAR = '■';

        public static char ReplaceNonASCII(char c) => c < 32 || c > 126 ? NON_ASCII_CHAR : c;

        public static void ReplaceNonASCII(string str, StringBuilder buffer) => str.ForEach(c => buffer.Append(ReplaceNonASCII(c)));
        public static string ReplaceNonASCII(string str) =>str.Aggregate(new StringBuilder(), (buffer, c) => buffer.Append(ReplaceNonASCII(c))).ToString();

        public static int HexDigitToInt(char c) {
            if (c >= '0' && c <= '9')
                return c - '0';
            else if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;
            else if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;
            else
                throw new ArgumentException($"Illegal hex digit: {c}");
        }

        public static char UnescapeChar(string escaped) {
            if (String.IsNullOrEmpty(escaped))
                throw new ArgumentException("Empty or null string");
            else if (escaped.Length == 1)
                return escaped[0];

            switch (escaped) {
                case "\\t":
                    return '\t';
                case "\\b":
                    return '\b';
                case "\\n":
                    return '\n';
                case "\\r":
                    return '\r';
                case "\\f":
                    return '\f';
                case "\\'":
                    return '\'';
                case "\\\"":
                    return '"';
                case "\\\\":
                    return '\\';
                default:
                    if (escaped.StartsWith("\\u") && escaped.Length == 6) {
                        return (char)(HexDigitToInt(escaped[2]) << 12
                            | HexDigitToInt(escaped[3]) << 8
                            | HexDigitToInt(escaped[4]) << 4
                            | HexDigitToInt(escaped[5]));
                    } else
                        throw new ArgumentException($"Invalid character: {escaped}");
            };
        }

        public static IEnumerable<string> GetCharRangeChars(string str) {
            var charRangeChars = new List<string>();
            for (int i = 0; i < str.Length; ++i) {
                char c = str[i];
                if (c == '\\') {
                    if (i == str.Length - 1)
                        throw new ArgumentException("Got backslash at end of quoted string");
                    if (str[i+1] == 'u') {
                        if (i > str.Length - 6)
                            throw new ArgumentException("Truncated Unicode character sequence");
                        charRangeChars.Add(UnescapeChar(str.Substring(i, 6)).ToString());
                        i += 5; // Consume escaped characters
                    } else {
                        var escapeSeq = str.Substring(i, 2);
                        if (new[] { "\\-", "\\^", "\\]", "\\\\" }.Contains(escapeSeq))
                            charRangeChars.Add(escapeSeq);
                         else
                            charRangeChars.Add(UnescapeChar(escapeSeq).ToString());
                        i++; // Consume escaped character
                    }
                } else
                    charRangeChars.Add(c.ToString());
            }
            return charRangeChars;
        }

        public static string UnescapeString(string str) {
            var buffer = new StringBuilder();
            for (int i = 0; i < str.Length; i++) {
                if (str[i] == '\\') {
                    if (i == str.Length - 1)
                        throw new ArgumentException("Got backslash at end of quoted string");
                    if (str[i+1] == 'u') {
                        if (i > str.Length - 6)
                            throw new ArgumentException("Truncated Unicode character sequence");
                        buffer.Append(UnescapeChar(str.Substring(i, 6)));
                        i += 5;
                    } else {
                        buffer.Append(UnescapeChar(str.Substring(i, 2)));
                        i++;
                    }
                } else {
                    buffer.Append(str[i]);
                }
            }
            return buffer.ToString();
        }

        public static string EscapeChar(char c) {
            if (c >= 32 && c <= 126)
                return c.ToString();
            else if (c == '\n')
                return "\\n";
            else if (c == '\r')
                return "\\r";
            else if (c == '\t')
                return "\\t";
            else if (c == '\f')
                return "\\f";
            else if (c == '\b')
                return "\\b";
            else
                return $"\\u{(int)c:X4}";
        }

        public static string EscapeQuotedChar(char c) {
            if (c == '\'')
                return "\\\'";
            else if (c == '\\')
                return "\\\\";
            else
                return EscapeChar(c);
        }

        public static string EscapeQuotedStringChar(char c) {
            if (c == '"')
                return "\\\"";
            else if (c == '\\')
                return "\\\\";
            else
                return EscapeChar(c);
        }

        public static string EscapeCharRangeChar(char c) {
            if (c == ']')
                return "\\]";
            else if (c == '^')
                return "\\^";
            else if (c == '-')
                return "\\-";
            else if (c == '\\')
                return "\\\\";
            else
                return EscapeChar(c);
        }

        public static string EscapeString(string str)
            => str.Aggregate(new StringBuilder(), (buffer, c) => buffer.Append(c == '"' ? "\\\"" : EscapeQuotedStringChar(c))).ToString();
    }
}
