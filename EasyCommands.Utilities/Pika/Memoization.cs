using EasyCommands.Utilities.Pika.Grammar;
using EasyCommands.Utilities.Pika.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasyCommands.Utilities.Pika.Memoization {

    public class Key : IEquatable<Key> {
        public Clause Clause { get; }
        public int Start { get; }

        public Key(Clause clause, int start) {
            Clause = clause;
            Start = start;
        }

        public bool Equals(Key other) => this == other || Clause == other.Clause && Start == other.Start;
        public override int GetHashCode() => Clause.GetHashCode() ^ Start;
        public override string ToString() => $"{Clause}:{Start}";
        public string ToStringWithRuleNames() => $"{Clause.ToStringWithRuleNames()}:{Start}";
    }

    public class Match {
        public static readonly IEnumerable<Match> NO_SUBCLAUSE_MATCHES = Enumerable.Empty<Match>();

        public struct Return { public string label; public Match match; }

        public Key Key { get; }
        public int Count { get; }

        int firstMatchingSubClauseIndex;
        readonly IEnumerable<Match> subClauseMatches;

        public Match(Key key) :this(key, 0) {}
        public Match(Key key, int count) : this(key, count, 0, NO_SUBCLAUSE_MATCHES) { }
        public Match(Key key, int count, IEnumerable<Match> matches) : this(key, count, 0, matches) { }
        public Match(Key key, int count, int start, IEnumerable<Match> matches) {
            Key = key;
            Count = count;
            firstMatchingSubClauseIndex = start;
            subClauseMatches = matches;
        }

        public IEnumerable<Return> GetSubClauseMatches() {
            if (!subClauseMatches.Any())
                return Enumerable.Empty<Return>();

            var clause = Key.Clause;
            if (clause is OneOrMore) {
                return this
                    .Descent(m => m.subClauseMatches.Take(2).Count() == 2, m => m.subClauseMatches.ElementAt(1))
                    .Select(m => new Return { label = m.Key.Clause.SubClauses.First().Label, match = m.subClauseMatches.First() });
            } else if (clause is First)
                return subClauseMatches
                    .Take(1)
                    .Select(m => new Return { label = clause.SubClauses.ElementAt(firstMatchingSubClauseIndex).Label, match = m });
            else
                return clause.SubClauses
                    .Select((m, i) => new Return { label = m.Label, match = subClauseMatches.ElementAt(i) });
        }
        public bool IsBetterThan(Match other) => this != other && Count > other.Count;

        public override string ToString() => $"{Key}+{Count}";
        public string ToStringWithRuleNames() => $"{Key.ToStringWithRuleNames()}+{Count}";
    }


    public class Table {
        Dictionary<Key, Match> table = new Dictionary<Key, Match>();

        public Grammar.Grammar Grammar { get; }
        public string Input { get; }
        public int NumMatchObjectsMemoized => numMatchObjectsMemoized;
        public int NumMatchObjectsCreated => numMatchObjectsCreated;

        int numMatchObjectsMemoized, numMatchObjectsCreated;

        public Table(Grammar.Grammar grammar, string input) {
            Grammar = grammar;
            Input = input;
        }

        public Match LookUpBestMatch(Key key) {
            table.TryGetValue(key, out Match match);

            var clause = key.Clause;
            return match
                ?? (clause is NotFollowedBy
                    ? clause.Match(this, key, Input)
                    : clause.CanMatchZeroChars
                        ? new Match(key)
                        : null);
        }

        public bool Add(Key key, Match match, List<Clause> priority) {
            var inserted = false;
            if (match != null) {
                Interlocked.Increment(ref numMatchObjectsCreated);

                table.TryGetValue(key, out Match old);

                if (old == null || match.IsBetterThan(old)) {
                    table[key] = match;
                    inserted = true;
                    Interlocked.Increment(ref numMatchObjectsMemoized);
                }
            }

            for (int i = 0, ii = key.Clause.seedParentClauses.Count; i < ii; ++i) {
                var seedParentClause = key.Clause.seedParentClauses[i];
                if (inserted || seedParentClause.CanMatchZeroChars) {
                    var index = priority.FindIndex(p => p.CompareTo(seedParentClause) > 0);
                    priority.Insert(index == -1 ? priority.Count : index, seedParentClause);
                }
            }
            return inserted;
        }

        public Dictionary<Clause, SortedDictionary<int, Match>> GetAllNavigableMatches()
            => table.Values
                .GroupBy(m => m.Key.Clause)
                .ToDictionary(g => g.Key, g => new SortedDictionary<int, Match>(g.ToDictionary(m => m.Key.Start)));

        public Dictionary<Clause, SortedDictionary<int, Match>> GetAllNonOverlappingMatches() {
            // ToDo: simplify  this somehow
            var dict = new Dictionary<Clause, SortedDictionary<int, Match>>();
            foreach (var kv in GetAllNavigableMatches()) {
                var cur = 0;
                var map = new SortedDictionary<int, Match>();
                foreach (var sm in kv.Value) {
                    var start = sm.Key;
                    if (start >= cur) {
                        var match = sm.Value;
                        map[start] = match;
                        cur = start + match.Count;
                    }
                }
                dict[kv.Key] = map;
            }
            return dict;
        }

        public SortedDictionary<int, Match> GetNavigableMatches(Clause clause)
            => new SortedDictionary<int, Match>(table.Where(p => p.Key.Clause == clause).ToDictionary(p => p.Key.Start, p => p.Value));

        public IEnumerable<Match> GetAllMatches(Clause clause)
            => GetNavigableMatches(clause).Values;

        public IEnumerable<Match> GetNonOverlappingMatches(Clause clause) {
            var matches = GetAllMatches(clause);
            var list = new List<Match>();
            for (int i = 0; i < matches.Count(); ++i) {
                var match = matches.ElementAt(i);
                var end = match.Key.Start + match.Count;
                list.Add(match);
                while (i < matches.Count() - 1 && matches.ElementAt(i + 1).Key.Start < end)
                    ++i;
            }
            return list;
        }


        public SortedDictionary<int, Tuple<int, string>> GetSyntaxErrors(params string[] syntaxCoverageRuleNames)
            => new SortedDictionary<int, Tuple<int, string>>(syntaxCoverageRuleNames
                .Select(s => Grammar.GetRule(s).LabeledClause.Clause)
                .SelectMany(c => GetNonOverlappingMatches(c))
                .ToIntervalUnion(m => m.Key.Start, m => m.Key.Start + m.Count)
                .Invert(0, Input.Length).NonOverlappingRanges
                .ToDictionary(kv => kv.Key, kv => Tuple.Create(kv.Value, Input.Substring(kv.Key, kv.Value - kv.Key))));
    }
}
