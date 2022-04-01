using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public interface IMatch {
            void Clear();
            bool SetValue(object p);
            bool Satisfied();
            bool Left(object p);
            bool Right(object p);
        }

        //Required Data Processors
        public class Match<T> : IMatch {
            T value;
            public bool left, right;
            public virtual T GetValue() => value;
            public virtual bool SetValue(object p) {
                var setValue = value == null && p is T;
                value = setValue ? (T)p : value;
                return setValue;
            }
            public bool Left(object o) => left && SetValue(o);
            public bool Right(object o) => right && SetValue(o);
            public virtual bool Satisfied() => value != null;
            public virtual void Clear() => value = default(T);
        }

        static Match<T> requiredRight<T>() => required<T>(false, true);
        static Match<T> requiredLeft<T>() => required<T>(true, false);
        static Match<T> requiredEither<T>() => required<T>(true, true);
        static Match<T> required<T>(bool left, bool right) => new Match<T> {
            left = left,
            right = right
        };

        //Optional Data Processors
        public class OptionalMatch<T> : Match<T> {
            public override bool Satisfied() => true;
        }

        static OptionalMatch<T> optionalRight<T>() => optional<T>(false, true);
        static OptionalMatch<T> optionalLeft<T>() => optional<T>(true, false);
        static OptionalMatch<T> optionalEither<T>() => optional<T>(true, true);
        static OptionalMatch<T> optional<T>(bool left, bool right) => new OptionalMatch<T> {
            left = left,
            right = right
        };

        //ListDataProcessors
        public class ListMatch<T> : Match<List<T>> {
            public bool required;
            List<T> values = NewList<T>();
            public override bool SetValue(object p) {
                if (p is T) values.Add((T)p);
                return p is T;
            }
            public override bool Satisfied() => !required || values.Count > 0;
            public override List<T> GetValue() => values;
            public override void Clear() => values.Clear();
        }

        static ListMatch<T> rightList<T>(bool required) => list<T>(false, true, required);
        static ListMatch<T> leftList<T>(bool required) => list<T>(true, false, required);
        static ListMatch<T> eitherList<T>(bool required) => list<T>(true, true, required);
        static ListMatch<T> list<T>(bool left, bool right, bool required) => new ListMatch<T> {
            left = left,
            right = right,
            required = required
        };

        static bool AllSatisfied(params IMatch[] processors) => processors.All(p => p.Satisfied());
    }
}
