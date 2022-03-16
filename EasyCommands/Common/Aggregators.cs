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
        public delegate Primitive Aggregator(IEnumerable<object> blocks, Func<object,Primitive> primitiveSupplier);
        public static Aggregator SumAggregator = (blocks, primitiveSupplier) => blocks.Select(primitiveSupplier).Aggregate((Primitive)null, (a, b) => a?.Plus(b) ?? b) ?? Primitive.From(0);

        public static bool Evaluate(int count, int matches, AggregationMode aggregation) {
            switch (aggregation) {
                case AggregationMode.ALL: return count > 0 && matches == count;
                case AggregationMode.ANY: return matches > 0;
                case AggregationMode.NONE: return matches == 0;
                default: throw new Exception("Unsupported Aggregation Mode");
            }
        }
    }
}
