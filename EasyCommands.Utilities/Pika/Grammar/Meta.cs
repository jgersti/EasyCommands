using EasyCommands.Utilities.Pika.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EasyCommands.Utilities.Pika.Utils.ClauseFactory;

namespace EasyCommands.Utilities.Pika.Grammar {
    public static class Meta {
        public static Grammar Grammar { get; }

        const string GRAMMAR = "GRAMMAR";
        const string WSC = "WSC";
        const string COMMENT = "COMMENT";
        const string RULE = "RULE";
        const string CLAUSE = "CLAUSE";
        const string IDENT = "IDENT";
        const string PREC = "PREC";
        const string NUM = "NUM";
        const string NAME_CHAR = "NAME_CHAR";
        const string CHAR_SET = "CHARSET";
        const string HEX = "Hex";
        const string CHAR_RANGE = "CHAR_RANGE";
        const string CHAR_RANGE_CHAR = "CHAR_RANGE_CHAR";
        const string QUOTED_STRING = "QUOTED_STR";
        const string ESCAPED_CTRL_CHAR = "ESCAPED_CTRL_CHAR";
        const string SINGLE_QUOTED_CHAR = "SINGLE_QUOTED_CHAR";
        const string STR_QUOTED_CHAR = "STR_QUOTED_CHAR";
        const string NOTHING = "NOTHING";
        const string START = "START";

        const string RULE_AST = "RuleAST";
        const string PREC_AST = "PrecAST";
        const string R_ASSOC_AST = "RAssocAST";
        const string L_ASSOC_AST = "LAssocAST";
        const string IDENT_AST = "IdentAST";
        const string LABEL_AST = "LabelAST";
        const string LABEL_NAME_AST = "LabelNameAST";
        const string LABEL_CLAUSE_AST = "LabelClauseAST";
        const string SEQ_AST = "SeqAST";
        const string FIRST_AST = "FirstAST";
        const string FOLLOWED_BY_AST = "FollowedByAST";
        const string NOT_FOLLOWED_BY_AST = "NotFollowedByAST";
        const string ONE_OR_MORE_AST = "OneOrMoreAST";
        const string ZERO_OR_MORE_AST = "ZeroOrMoreAST";
        const string OPTIONAL_AST = "OptionalAST";
        const string SINGLE_QUOTED_CHAR_AST = "SingleQuotedCharAST";
        const string CHAR_RANGE_AST = "CharRangeAST";
        const string QUOTED_STRING_AST = "QuotedStringAST";
        const string START_AST = "StartAST";
        const string NOTHING_AST = "NothingAST";

        static readonly Dictionary<Type, int> clauseTypeToPrecedence = new Dictionary<Type, int>();

        static Meta() {
            clauseTypeToPrecedence[typeof(Terminal)] = 7;
            clauseTypeToPrecedence[typeof(RuleRef)] = 7;
            clauseTypeToPrecedence[typeof(OneOrMore)] = 6;
            // ZeroOrMore = 6
            clauseTypeToPrecedence[typeof(NotFollowedBy)] = 5;
            clauseTypeToPrecedence[typeof(FollowedBy)] = 5;
            // Optional = 4
            clauseTypeToPrecedence[typeof(ASTNodeLabel)] = 3;
            clauseTypeToPrecedence[typeof(Seq)] = 2;
            clauseTypeToPrecedence[typeof(First)] = 1;

            Grammar = new Grammar(new List<Rule> {
                Rule(GRAMMAR,
                    Seq(Start(), RuleRef(WSC), OneOrMore(RuleRef(RULE)))),
                Rule(RULE, Ast(RULE_AST,
                    Seq(RuleRef(IDENT), RuleRef(WSC), Optional(RuleRef(PREC)), Str("<-"), RuleRef(WSC), RuleRef(CLAUSE), RuleRef(WSC), Char(';'), RuleRef(WSC)))),

                Rule(CLAUSE, 8, null,
                    Seq(Char('('), RuleRef(WSC), RuleRef(CLAUSE), RuleRef(WSC), Char(')'))),
                Rule(CLAUSE, 7, null,
                    First(
                        RuleRef(IDENT),
                        RuleRef(QUOTED_STRING),
                        RuleRef(CHAR_SET),
                        RuleRef(NOTHING),
                        RuleRef(START))),
                Rule(CLAUSE, 6, null,
                    First(
                        Seq(Ast(ONE_OR_MORE_AST, RuleRef(CLAUSE)), RuleRef(WSC), Char('+')),
                        Seq(Ast(ZERO_OR_MORE_AST, RuleRef(CLAUSE)), RuleRef(WSC), Char('*')))),
                Rule(CLAUSE, 5, null,
                    First(
                        Seq(Char('&'), Ast(FOLLOWED_BY_AST, RuleRef(CLAUSE))),
                        Seq(Char('!'), Ast(NOT_FOLLOWED_BY_AST, RuleRef(CLAUSE))))),
                Rule(CLAUSE, 4, null,
                    Seq(Ast(OPTIONAL_AST, RuleRef(CLAUSE)), RuleRef(WSC), Char('?'))),
                Rule(CLAUSE, 3, null,
                    Ast(LABEL_AST,
                        Seq(Ast(LABEL_NAME_AST, RuleRef(IDENT)), RuleRef(WSC), Char(':'), RuleRef(WSC), Ast(LABEL_CLAUSE_AST, RuleRef(CLAUSE)), RuleRef(WSC)))),
                Rule(CLAUSE, 2, null,
                    Ast(SEQ_AST,
                        Seq(RuleRef(CLAUSE), RuleRef(WSC), OneOrMore(Seq(RuleRef(CLAUSE), RuleRef(WSC)))))),
                Rule(CLAUSE, 1, null,
                    Ast(FIRST_AST,
                        Seq(RuleRef(CLAUSE), RuleRef(WSC), OneOrMore(Seq(Char('/'), RuleRef(WSC), RuleRef(CLAUSE), RuleRef(WSC)))))),

                Rule(WSC,
                    ZeroOrMore(First(Char(' ', '\n', '\r', '\t'), RuleRef(COMMENT)))),

                Rule(COMMENT,
                    Seq(Char('#'), ZeroOrMore(Char('\n').Invert()))),

                Rule(IDENT,
                    Ast(IDENT_AST,
                        Seq(RuleRef(NAME_CHAR), ZeroOrMore(First(RuleRef(NAME_CHAR), CharRange('0', '9')))))),

                Rule(NUM,
                    OneOrMore(CharRange('0', '9'))),

                Rule(NAME_CHAR,
                    Char(CharRange('a', 'z'), CharRange('A', 'Z'), Char('_', '-'))),

                Rule(PREC,
                    Seq(Char('['), RuleRef(WSC), Ast(PREC_AST, RuleRef(NUM)), RuleRef(WSC),
                        Optional(Seq(Char(','), RuleRef(WSC), First(
                            Ast(R_ASSOC_AST, First(Char('r'), Char('R'))),
                            Ast(L_ASSOC_AST, First(Char('l'), Char('L')))),
                        RuleRef(WSC))), Char(']'), RuleRef(WSC))),

                Rule(CHAR_SET,
                    First(
                        Seq(Char('\''), Ast(SINGLE_QUOTED_CHAR_AST, RuleRef(SINGLE_QUOTED_CHAR)), Char('\'')),
                        Seq(Char('['), Ast(CHAR_RANGE_AST, Seq(Optional(Char('^')), OneOrMore(First(RuleRef(CHAR_RANGE), RuleRef(CHAR_RANGE_CHAR))))), Char(']')))),

                Rule(SINGLE_QUOTED_CHAR,
                    First(
                        RuleRef(ESCAPED_CTRL_CHAR),
                        Char('\'').Invert())), // ***

                Rule(CHAR_RANGE,
                    Seq(RuleRef(CHAR_RANGE_CHAR), Char('-'), RuleRef(CHAR_RANGE_CHAR))),

                Rule(CHAR_RANGE_CHAR,
                    First(
                        Char('\\', ']').Invert(),
                        RuleRef(ESCAPED_CTRL_CHAR),
                        Str("\\-"),
                        Str("\\\\"),
                        Str("\\]"),
                        Str("\\^"))),

                Rule(QUOTED_STRING,
                    Seq(Char('"'), Ast(QUOTED_STRING_AST, ZeroOrMore(RuleRef(STR_QUOTED_CHAR))), Char('"'))),

                Rule(STR_QUOTED_CHAR,
                    First(
                        RuleRef(ESCAPED_CTRL_CHAR),
                        Char('"', '\\').Invert())),

                Rule(HEX,
                    Char(CharRange('0', '9'), CharRange('a', 'f'), CharRange('A', 'F'))),

                Rule(ESCAPED_CTRL_CHAR,
                    First(
                        Str("\\t"),
                        Str("\\b"),
                        Str("\\n"),
                        Str("\\r"),
                        Str("\\f"),
                        Str("\\'"),
                        Str("\\\""),
                        Str("\\\\"),
                        Seq(Str("\\u"), RuleRef(HEX), RuleRef(HEX), RuleRef(HEX), RuleRef(HEX)))),

                Rule(NOTHING,
                    Ast(NOTHING_AST, Seq(Char('('), RuleRef(WSC), Char(')')))),

                Rule(START,
                    Ast(START_AST, Char('^')))
            });
        }

        public static bool NeedToAddParensAroundSubClause(Clause parent, Clause child)
            => (parent is First && child is Seq)
                || (clauseTypeToPrecedence[child is Terminal ? typeof(Terminal) : child.GetType()]
                    <= clauseTypeToPrecedence[parent is Terminal ? typeof(Terminal) : parent.GetType()]);
        public static bool NeedToAddParensAroundASTNodeLabels(Clause clause)
            => clauseTypeToPrecedence[clause is Terminal ? typeof(Terminal) : clause.GetType()] < clauseTypeToPrecedence[typeof(ASTNodeLabel)];

        static Clause ExpectOne(IEnumerable<Clause> clauses, AST.Node node)  {
            try {
                return clauses.Single();
            } catch (InvalidOperationException) {
                throw new ArgumentException($"Expected one subclause, got {clauses.Count()}: {node}");
            }
        }

        static IEnumerable<Clause> ParseASTNodes(IEnumerable<AST.Node> nodes) => nodes.Select(ParseASTNode);
        static Clause ParseASTNode(AST.Node node) {
            switch(node.label) {
                case SEQ_AST:
                    return Seq(ParseASTNodes(node.children).ToArray());
                case FIRST_AST:
                    return First(ParseASTNodes(node.children).ToArray());
                case ONE_OR_MORE_AST:
                    return OneOrMore(ExpectOne(ParseASTNodes(node.children), node));
                case ZERO_OR_MORE_AST:
                    return ZeroOrMore(ExpectOne(ParseASTNodes(node.children), node));
                case OPTIONAL_AST:
                    return Optional(ExpectOne(ParseASTNodes(node.children), node));
                case FOLLOWED_BY_AST:
                    return FollowedBy(ExpectOne(ParseASTNodes(node.children), node));
                case NOT_FOLLOWED_BY_AST:
                    return NotFollowedBy(ExpectOne(ParseASTNodes(node.children), node));
                case LABEL_AST:
                    return Ast(node.First().GetText(), ParseASTNode(node.ElementAt(1).First()));
                case IDENT_AST:
                    return RuleRef(node.GetText());
                case QUOTED_STRING_AST:
                    return Str(StringUtils.UnescapeString(node.GetText()));
                case SINGLE_QUOTED_CHAR_AST:
                    return Char(StringUtils.UnescapeChar(node.GetText()));
                case START_AST:
                    return Start();
                case NOTHING_AST:
                    return Nothing();
                case CHAR_RANGE_AST:
                    return CharRange(node.GetText());
                default:
                    return ExpectOne(ParseASTNodes(node.children), node);
            }
        }

        static Rule ParseRule(AST.Node node) {
            string name = node.First().GetText();
            var hasPrec = node.children.Count > 2;
            var assoc = node.children.Count < 4
                ? null
                : node.ElementAt(2).label.Equals(L_ASSOC_AST)
                    ? Associativity.LEFT
                    : node.ElementAt(2).label.Equals(R_ASSOC_AST)
                        ? Associativity.RIGHT
                        : (Associativity?)null;
            var prec = hasPrec ? int.Parse(node.ElementAt(1).GetText()) : -1;
            if (hasPrec && prec < 0)
                throw new ArgumentException($"Precedence needs to be zero or positive (rule {name} has precedence level {prec})");
            return Rule(name, prec, assoc, ParseASTNode(node.Last()));
        }

        public static Grammar Parse(string input) {
            var table = Grammar.Parse(input);

            //ParserInfo.PrintParseResult(GRAMMAR, table, new[] { GRAMMAR, RULE, $"{CLAUSE}[1]" }, false);
            //Console.WriteLine("\nParsed meta-grammar:");
            //Grammar.AllClauses
            //    .ForEach(c => Console.WriteLine($"    {c.ToStringWithRuleNames()}"));

            ParserInfo.PrintSyntaxErrors(table.GetSyntaxErrors(GRAMMAR, RULE, $"{CLAUSE}[{clauseTypeToPrecedence[typeof(First)]}]"));

            var topLabel = Grammar.GetRule(GRAMMAR).LabeledClause.Label ?? "<root>";
            var topMatches = Grammar.GetNonOverlappingMatches(GRAMMAR, table);
            if (!topMatches.Any())
                throw new ArgumentException($"Top level rule \"{GRAMMAR}\" did not match");
            else if (topMatches.Count() > 1) {
                Console.WriteLine("\nMultiple top level matches");
                topMatches
                    .Select(m => new AST.Node(topLabel, m, input))
                    .ForEach(Console.WriteLine);
                throw new InvalidOperationException("Stopping.");
            }

            var topMatch = topMatches.Single();
            // TreeUtils.PrintTreeView(topMatch, input);

            var topNode = new AST.Node(topLabel, topMatch, input);
            //Console.WriteLine(topNode);

            if (topNode.Any(n => !n.label.Equals(RULE_AST)))
                throw new InvalidOperationException("Wrong node type");

            return new Grammar(topNode.Select(ParseRule).ToList());
        }
    }
}
