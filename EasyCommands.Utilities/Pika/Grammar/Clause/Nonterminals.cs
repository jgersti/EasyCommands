using EasyCommands.Utilities.Pika.Memoization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCommands.Utilities.Pika.Grammar {
    public class First : Clause {
        public First(params Clause[] subClauses) : base(subClauses) {
            if (subClauses.Length < 2)
                throw new ArgumentException($"{nameof(First)} expects 2 or more subclauses.");
        }

        public override void DetermineWhetherCanMatchZeroChars() {
            for (int i = 0; i < SubClauses.Count(); ++i)
                if (SubClauses.ElementAt(i).Clause.CanMatchZeroChars) {
                    CanMatchZeroChars = true;
                    if (i < SubClauses.Count() - 1)
                        throw new InvalidOperationException($"Subclause {i} of {nameof(First)} can match zero charachters, which means subsequent subclauses will never be matched: {this}");
                    break;
                }
        }

        public override Match Match(Table table, Key key, string input) {
            for (int i = 0; i < SubClauses.Count(); ++i) {
                var match = table.LookUpBestMatch(new Key(SubClauses.ElementAt(i).Clause, key.Start));
                if (match != null)
                    return new Match(key, match.Count, i, new Match[] { match });
            }
            return null;
        }

        public override string ToString()
            => ToStringCached = ToStringCached ?? string.Join(" / ", SubClauses.Select(c => c.ToStringWithASTNodeLabel(this)));
    }

    public class FollowedBy : Clause {
        public FollowedBy(Clause clause) : base(clause) {}

        public override void DetermineWhetherCanMatchZeroChars() {
            if (SubClauses.First().Clause.CanMatchZeroChars)
                throw new InvalidOperationException($"Subclause alway matches zero characters, so this clause will never match anything: {this}");
        }

        public override Match Match(Table table, Key key, string input) {
            var match = table.LookUpBestMatch(new Key(SubClauses.First().Clause, key.Start));
            if (match != null)
                return new Match(key);
            return null;
        }

        public override string ToString()
            => ToStringCached = ToStringCached ?? $"&{SubClauses.First().ToStringWithASTNodeLabel(this)}";
    }

    public class NotFollowedBy : Clause {
        public NotFollowedBy(Clause clause) : base(clause) { }

        public override void DetermineWhetherCanMatchZeroChars() {
            if (SubClauses.First().Clause.CanMatchZeroChars)
                throw new InvalidOperationException($"Subclause alway matches zero characters, so this clause will never match anything: {this}");
        }

        public override Match Match(Table table, Key key, string input) {
            var match = table.LookUpBestMatch(new Key(SubClauses.First().Clause, key.Start));
            return match ?? new Match(key);
        }

        public override string ToString()
            => ToStringCached = ToStringCached ?? $"!{SubClauses.First().ToStringWithASTNodeLabel(this)}";
    }

    public class OneOrMore : Clause {
        public OneOrMore(Clause clause) : base(clause) { }

        public override void DetermineWhetherCanMatchZeroChars()
            => CanMatchZeroChars = SubClauses.First().Clause.CanMatchZeroChars;

        public override Match Match(Table table, Key key, string input) {
            var match = table.LookUpBestMatch(new Key(SubClauses.First().Clause, key.Start));
            if (match == null)
                return null;

            var tail = table.LookUpBestMatch(new Key(this, key.Start + match.Count));
            return tail == null
                ? new Match(key, match.Count, new Match[] { match })
                : new Match(key, match.Count + tail.Count, new Match[] { match, tail });
        }

        public override string ToString()
            => ToStringCached = ToStringCached ?? $"{SubClauses.First().ToStringWithASTNodeLabel(this)}+";
    }

    public class Seq : Clause {
        public Seq(params Clause[] subClauses) : base(subClauses) {
            if (subClauses.Length < 2)
                throw new ArgumentException($"{nameof(First)} expects 2 or more subclauses.");
        }

        public override void DetermineWhetherCanMatchZeroChars()
            => CanMatchZeroChars = SubClauses.All(c => c.Clause.CanMatchZeroChars);

        public override void AddAsSeedParentClause() {
            var added = new HashSet<Clause>();
            foreach (var clause in SubClauses.Select(c => c.Clause)) {
                if (added.Add(clause))
                    clause.seedParentClauses.Add(this);
                if (!clause.CanMatchZeroChars)
                    break;
            }
        }

        public override Match Match(Table table, Key key, string input) {
            int cur = key.Start;
            List<Match> matches = null;
            for (int i = 0; i < SubClauses.Count(); ++i) {
                var match = table.LookUpBestMatch(new Key(SubClauses.ElementAt(i).Clause, key.Start));
                if (match == null)
                    return null;
                if (matches == null)
                    matches = new List<Match>(SubClauses.Count());
                matches.Add(match);
                cur += match.Count;
            }

            return new Match(key, cur - key.Start, matches.ToArray());
        }

        public override string ToString()
            => ToStringCached = ToStringCached ?? string.Join(" ", SubClauses.Select(c => c.ToStringWithASTNodeLabel(this)));
    }


}
