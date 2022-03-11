using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyCommands.Utilities.Pika.Grammar;
using EasyCommands.Utilities.Pika.Memoization;

namespace EasyCommands.Utilities.Pika.Utils {
    public static class ParserInfo {
        public static void PrintClauses(Grammar.Grammar grammar)
            => grammar.AllClauses
                .Select((c, i) => $"{i:D3}: {c.ToStringWithRuleNames()}")
                .Reverse()
                .ForEach(s => Console.WriteLine(s));

        public static void PrintRules(Grammar.Grammar grammar)
            => grammar.AllRules
                .Select((r, i) => $"{i:D3}: {r}")
                .ForEach(s => Console.WriteLine(s));

        public static void PrintMemoTable(Table table) {
            StringBuilder[] buffers = new StringBuilder[table.Grammar.AllClauses.Count()];
            int marginWidth = 0;
            for (int i = 0; i < table.Grammar.AllClauses.Count(); ++i) {
                buffers[i] = new StringBuilder();
                buffers[i].Append($"{table.Grammar.AllClauses.Count() - 1 - i:D3} : ");
                Clause clause = table.Grammar.AllClauses.ElementAt(table.Grammar.AllClauses.Count() - 1 - i);
                if (clause is Terminal)
                    buffers[i].Append("[terminal] ");

                if (clause.CanMatchZeroChars)
                    buffers[i].Append("[canMatchZeroChars] ");

                buffers[i].Append(clause.ToStringWithRuleNames());
                marginWidth = Math.Max(marginWidth, buffers[i].Length + 2);
            }

            int tableWidth = marginWidth + table.Input.Length + 1;
            for (int i = 0; i < table.Grammar.AllClauses.Count(); ++i) {
                while (buffers[i].Length < marginWidth)
                    buffers[i].Append(' ');

                while (buffers[i].Length < tableWidth)
                    buffers[i].Append('-');
            }

            var nonOverlappingMatches = table.GetAllNonOverlappingMatches();
            for (var clauseIdx = table.Grammar.AllClauses.Count() - 1; clauseIdx >= 0; --clauseIdx) {
                var row = table.Grammar.AllClauses.Count() - 1 - clauseIdx;
                var clause = table.Grammar.AllClauses.ElementAt(clauseIdx);

                nonOverlappingMatches.TryGetValue(clause, out SortedDictionary<int, Match> matchesForClause);

                foreach (var match in matchesForClause?.Values ?? Enumerable.Empty<Match>()) {
                    var matchStartPos = match.Key.Start;
                    var matchEndPos = matchStartPos + match.Count;
                    if (matchStartPos <= table.Input.Length) {
                        buffers[row][marginWidth + matchStartPos] = '#';
                        for (int j = matchStartPos + 1; j < matchEndPos; j++)
                            if (j <= table.Input.Length)
                                buffers[row][marginWidth + j] = '=';
                    }
                }
                Console.WriteLine(buffers[row]);
            }


            for (int j = 0; j < marginWidth; ++j)
                Console.Write(' ');
            for (int i = 0; i < table.Input.Length; ++i)
                Console.Write(i % 10);
            Console.WriteLine();

            for (int i = 0; i < marginWidth; ++i)
                Console.Write(' ');
            Console.WriteLine(StringUtils.ReplaceNonASCII(table.Input));
        }

        private static int FindCycleDepth(Match match, IDictionary<int, IDictionary<int, IDictionary<int, Match>>> cycleDepthToMatches) {
                var cycleDepth = 0;
                foreach (var subClauseMatch in match.GetSubClauseMatches().Select(r => r.match)) {
                    var subClauseIsInDifferentCycle = match.Key.Clause.Rank <= subClauseMatch.Key.Clause.Rank;
                    var subClauseMatchDepth = FindCycleDepth(subClauseMatch, cycleDepthToMatches);
                    cycleDepth = Math.Max(cycleDepth, subClauseIsInDifferentCycle ? subClauseMatchDepth + 1 : subClauseMatchDepth);
                }
                var matchesForDepth = cycleDepthToMatches.GetOrCreateValue(cycleDepth, k
                        => new SortedDictionary<int, IDictionary<int, Match>>(Comparer<int>.Create((x, y) => y.CompareTo(x))));

                var matchesForClauseIdx = matchesForDepth.GetOrCreateValue(match.Key.Clause.Rank, k
                        => new SortedDictionary<int, Match>(Comparer<int>.Create((x, y) => y.CompareTo(x))));

                matchesForClauseIdx[match.Key.Start] = match;
                return cycleDepth;
        }

        public static void PrintParseTreeInMemoTableForm(Table table) {
            if (!table.Grammar.AllClauses.Any()) {
                throw new ArgumentException("Grammar is empty");
            }

            // Map from cycle depth (sorted in decreasing order) => clauseIdx => startPos => match
            var cycleDepthToMatches = new SortedDictionary<int, IDictionary<int, IDictionary<int, Match>>>(Comparer<int>.Create((x, y) => y.CompareTo(x)));

            // Input spanned by matches found so far
            var inputSpanned = new IntervalUnion();

            var nonOverlappingMatches = table.GetAllNonOverlappingMatches();
            var maxCycleDepth = 0;
            for (var clauseIdx = table.Grammar.AllClauses.Count() - 1; clauseIdx >= 0; --clauseIdx) {
                var clause = table.Grammar.AllClauses.ElementAt(clauseIdx);
                nonOverlappingMatches.TryGetValue(clause, out SortedDictionary<int, Match> matchesForClause);
                foreach (var match in matchesForClause?.Values ?? Enumerable.Empty<Match>()) {
                    var matchStartPos = match.Key.Start;
                    var matchEndPos = matchStartPos + match.Count;
                    if (!inputSpanned.RangeOverlaps(matchStartPos, matchEndPos)) {
                        var cycleDepth = FindCycleDepth(match, cycleDepthToMatches);
                        maxCycleDepth = Math.Max(maxCycleDepth, cycleDepth);
                        inputSpanned.AddRange(matchStartPos, matchEndPos);
                    }
                }
            }

            // Assign matches to rows
            List<IDictionary<int, Match>> matchesForRow = new List<IDictionary<int, Match>>();
            List<Clause> clauseForRow = new List<Clause>();
            foreach (var matchesForDepth in cycleDepthToMatches.Values) {
                foreach (var matchesForClauseIdxEnt in matchesForDepth) {
                    clauseForRow.Add(table.Grammar.AllClauses.ElementAt(matchesForClauseIdxEnt.Key));
                    matchesForRow.Add(matchesForClauseIdxEnt.Value);
                }
            }

            var rowLabel = new StringBuilder[clauseForRow.Count()];
            var rowLabelMaxWidth = 0;
            for (var i = 0; i < clauseForRow.Count(); i++) {
                var clause = clauseForRow.ElementAt(i);
                rowLabel[i] = new StringBuilder();
                if (clause is Terminal) {
                    rowLabel[i].Append("[terminal] ");
                }
                if (clause.CanMatchZeroChars) {
                    rowLabel[i].Append("[canMatchZeroChars] ");
                }
                rowLabel[i].Append(clause.ToStringWithRuleNames());
                rowLabel[i].Append("  ");
                rowLabelMaxWidth = Math.Max(rowLabelMaxWidth, rowLabel[i].Length);
            }

            for (var i = 0; i < clauseForRow.Count(); i++) {
                var clause = clauseForRow.ElementAt(i);
                var clauseIdx = clause.Rank;
                string label = rowLabel[i].ToString();
                rowLabel[i].Clear();
                for (int j = 0, jj = rowLabelMaxWidth - label.Length; j < jj; j++)
                    rowLabel[i].Append(' ');

                rowLabel[i].Append($"{clauseIdx:D3} : ");
                rowLabel[i].Append(label);
            }
            var emptyRowLabel = new StringBuilder();
            for (int i = 0, ii = rowLabelMaxWidth + 6; i < ii; i++) {
                emptyRowLabel.Append(' ');
            }
            var edgeMarkers = new StringBuilder();
            edgeMarkers.Append(' ');
            for (int i = 1, ii = table.Input.Length * 2; i < ii; i++) {
                edgeMarkers.Append('\u2591');
            }
            // Append one char for last column boundary, and two extra chars for zero-length matches past end of string
            edgeMarkers.Append("   ");

            // Add tree structure to right of row label
            for (var row = 0; row < clauseForRow.Count(); row++) {
                var matches = matchesForRow.ElementAt(row);

                StringBuilder rowTreeChars = new StringBuilder();
                rowTreeChars.Append(edgeMarkers);
                var zeroLenMatchIdxs = new List<int>();
                foreach (var match in matches.Values) {
                    var startIdx = match.Key.Start;
                    var endIdx = startIdx + match.Count;

                    if (startIdx == endIdx) {
                        // Zero-length match
                        zeroLenMatchIdxs.Add(startIdx);
                    } else {
                        // Match consumes 1 or more characters
                        for (var i = startIdx; i <= endIdx; i++) {
                            char chrLeft = rowTreeChars[i * 2];
                            rowTreeChars[i * 2] =
                                    i == startIdx
                                            ? (chrLeft == '│' ? '├' : chrLeft == '┤' ? '┼' : chrLeft == '┐' ? '┬' : '┌')
                                            : i == endIdx ? (chrLeft == '│' ? '┤' : '┐') : '─';
                            if (i < endIdx) {
                                rowTreeChars[i * 2 + 1] = '─';
                            }
                        }
                    }
                }
                Console.Write(emptyRowLabel);
                Console.WriteLine(rowTreeChars);

                foreach (var match in matches.Values) {
                    var startIdx = match.Key.Start;
                    var endIdx = startIdx + match.Count;
                    edgeMarkers[startIdx * 2] = '│';
                    edgeMarkers[endIdx * 2] = '│';
                    for (int i = startIdx * 2 + 1, ii = endIdx * 2; i < ii; i++) {
                        var c = edgeMarkers[i];
                        if (c == '░' || c == '│') {
                            edgeMarkers[i] = ' ';
                        }
                    }
                }
                rowTreeChars.Clear();
                rowTreeChars.Append(edgeMarkers);
                foreach (var match in matches.Values) {
                    var startIdx = match.Key.Start;
                    var endIdx = startIdx + match.Count;
                    for (int i = startIdx; i < endIdx; i++) {
                        rowTreeChars[i * 2 + 1] = StringUtils.ReplaceNonASCII(table.Input[i]);
                    }
                }
                foreach (var zeroLenMatchIdx in zeroLenMatchIdxs)
                    rowTreeChars[zeroLenMatchIdx * 2] = '▮';
                Console.Write(rowLabel[row]);
                Console.WriteLine(rowTreeChars);
            }

            for (int j = 0; j < rowLabelMaxWidth + 6; j++)
                Console.Write(' ');

            Console.Write(' ');
            for (int i = 0; i < table.Input.Length; i++) {
                Console.Write(i % 10);
                Console.Write(' ');
            }
            Console.WriteLine();

            for (int i = 0; i < rowLabelMaxWidth + 6; i++)
                Console.Write(' ');

            Console.Write(' ');
            for (int i = 0; i < table.Input.Length; i++) {
                Console.Write(StringUtils.ReplaceNonASCII(table.Input[i]));
                Console.Write(' ');
            }
            Console.WriteLine();
        }


        public static void PrintSyntaxErrors(SortedDictionary<int, Tuple<int, string>> syntaxErrors) {
            if (syntaxErrors.Any()) {
                Console.WriteLine("\nSYNTAX ERRORS:\n");
                foreach (var e in syntaxErrors) {
                    var startPos = e.Key;
                    var endPos = e.Value.Item1;
                    var syntaxErrStr = e.Value.Item2;
                    Console.WriteLine($"{startPos}+{endPos - startPos} : {StringUtils.ReplaceNonASCII(syntaxErrStr)}");
                }
            }
        }

        public static void PrintMatches(Clause clause, Table table, bool showAllMatches) {
            var matches = table.GetAllMatches(clause);
            if (matches.Any()) {
                Console.WriteLine($"\n====================================\n\nMatches for {clause.ToStringWithRuleNames()} :");
                string label = string.Join(":", clause.rules?.Select(r => r.LabeledClause.Label) ?? Enumerable.Empty<string>());
                var prevEndPos = -1;
                for (int j = 0; j < matches.Count(); j++) {
                    var match = matches.ElementAt(j);
                    var overlapsPrevMatch = match.Key.Start < prevEndPos;
                    if (!overlapsPrevMatch || showAllMatches) {
                        var indent = overlapsPrevMatch ? "    " : "";
                        var buffer = new StringBuilder();
                        TreeUtils.RenderTreeView(match, string.IsNullOrEmpty(label) ? null : label, table.Input, indent, true, buffer);
                        Console.WriteLine(buffer);
                    }
                    int newEndPos = match.Key.Start + match.Count;
                    if (newEndPos > prevEndPos) {
                        prevEndPos = newEndPos;
                    }
                }
            } else
                Console.WriteLine($"\n====================================\n\nNo matches for {clause.ToStringWithRuleNames()}");
        }

        public static void PrintMatchesAndSubClauseMatches(Clause clause, Table memoTable) {
            PrintMatches(clause, memoTable, true);
            clause.SubClauses.ForEach(c => PrintMatches(c.Clause, memoTable, true));
        }

        public static void PrintMatchesAndPartialMatches(Seq seqClause, Table table) {
            var numSubClauses = seqClause.SubClauses.Count();
            foreach (var subClause0Match in table.GetAllMatches(seqClause.SubClauses.First().Clause)) {
                var subClauseMatches = new List<Match> { subClause0Match };
                var currStartPos = subClause0Match.Key.Start + subClause0Match.Count;
                subClauseMatches.AddRange(seqClause.SubClauses
                        .Select(c => table.LookUpBestMatch(new Key(c.Clause, currStartPos)))
                        .TakeWhile(m => m != null));

                Console.WriteLine("\n====================================\n\nMatched "
                        + (subClauseMatches.Count() == numSubClauses ? "all subclauses"
                                : subClauseMatches.Count() + " out of " + numSubClauses + " subclauses")
                        + " of clause (" + seqClause + ") at start pos " + subClause0Match.Key.Start);
                Console.WriteLine();

                for (int i = 0; i < subClauseMatches.Count(); i++) {
                    var subClauseMatch = subClauseMatches.ElementAt(i);
                    var buffer = new StringBuilder();
                    TreeUtils.RenderTreeView(subClauseMatch, seqClause.SubClauses.ElementAt(i).Label, table.Input, "", true, buffer);
                    Console.WriteLine(buffer);
                }
            }
        }

        public static void PrintAST(string label, Clause clause, Table table)
            => table.GetNonOverlappingMatches(clause)
                .ForEach(m => Console.WriteLine(new AST.Node(label, m, table.Input)));

        public static void PrintParseResult(string topLevelRuleName, Table table, string[] syntaxCoverageRuleNames, bool showAllMatches) {
            Console.WriteLine();
            Console.WriteLine("Clauses:");
            PrintClauses(table.Grammar);

            Console.WriteLine();
            Console.WriteLine("Memo Table:");
            PrintMemoTable(table);

            // Print memo table
            Console.WriteLine();
            Console.WriteLine("Match tree for rule " + topLevelRuleName + ":");
            PrintParseTreeInMemoTableForm(table);

            table.Grammar.AllClauses
                .Reverse()
                .ForEach(c => PrintMatches(c, table, showAllMatches));

            table.Grammar.ruleNameWithPrecedenceToRule.TryGetValue(topLevelRuleName, out Rule rule);
            if (rule != null) {
                Console.WriteLine($"\n====================================\n\nAST for rule \"{topLevelRuleName}\":\n");
                PrintAST(topLevelRuleName, rule.LabeledClause.Clause, table);
            } else
                Console.WriteLine($"\nRule \"{topLevelRuleName}\" does not exist");

            var syntaxErrors = table.GetSyntaxErrors(syntaxCoverageRuleNames);
            if (syntaxErrors.Any())
                PrintSyntaxErrors(syntaxErrors);

            Console.WriteLine("\nNum match objects created: " + table.NumMatchObjectsCreated);
            Console.WriteLine("Num match objects memoized:  " + table.NumMatchObjectsMemoized);
        }
    }
}
