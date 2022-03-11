using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCommands.Utilities.Pika.Grammar {
    public enum Associativity { LEFT, RIGHT }

    public class Rule {
        public string Name { get; set; }
        public int Precedence { get; }
        public Associativity? Associativity { get; }
        public AST.LabeledClause LabeledClause { get; }

        public Rule(string name, Clause clause) : this(name, -1, null, clause) {}
        public Rule(string name, int precedence, Associativity? associativity, Clause clause) {
            Name = name;
            Precedence = precedence;
            Associativity = associativity;

            var nodeLabel = clause as ASTNodeLabel;
            LabeledClause = new AST.LabeledClause(nodeLabel?.SubClauses.First().Clause ?? clause, nodeLabel?.Label);
        }

        public override string ToString() => $"{Name} <- {LabeledClause}";
    }
}
