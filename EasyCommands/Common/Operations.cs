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
        public delegate Primitive UnaryOperation(Primitive a);
        public delegate Primitive BinaryOperation(Primitive a, Primitive b);

        Dictionary<MyTuple<UnaryOperator, Return>, UnaryOperation> UnaryOperations = NewDictionary<MyTuple<UnaryOperator, Return>, UnaryOperation>();
        Dictionary<MyTuple<BinaryOperator, Return, Return>, BinaryOperation> BinaryOperations = NewDictionary<MyTuple<BinaryOperator, Return, Return>, BinaryOperation>();

        static readonly Dictionary<UnaryOperator, string> UnaryOperatorToString = NewDictionary(
                KeyValuePair(UnaryOperator.CAST, "cast"),
                KeyValuePair(UnaryOperator.ROUND, "round"),
                KeyValuePair(UnaryOperator.REVERSE, "negate")
            );

        static readonly Dictionary<BinaryOperator, string> BinaryOperatorToString = NewDictionary(
                KeyValuePair(BinaryOperator.CAST, "cast"),
                KeyValuePair(BinaryOperator.ROUND, "round"),
                KeyValuePair(BinaryOperator.COMPARE, "compare")
            );

        public Primitive PerformOperation(UnaryOperator type, Primitive a) =>
            UnaryOperations.GetValueOrDefault(MyTuple.Create(type, a.returnType), p => {
                throw new Exception("Cannot perform operation: " + UnaryOperatorToString[type] + " on type: " + ReturnToString[p.returnType]);
            })(a);

        public Primitive PerformOperation(BinaryOperator type, Primitive a, Primitive b) =>
            BinaryOperations.GetValueOrDefault(MyTuple.Create(type, a.returnType, b.returnType), (p, q) => {
                throw new Exception("Cannot perform operation: " + BinaryOperatorToString[type] + " on types: " + ReturnToString[p.returnType] + ", " + ReturnToString[q.returnType]);
            })(a, b);

        void AddUniOperation<T>(UnaryOperator type, Func<T,object> resolver) {
            foreach (Return t in GetTypes(typeof(T)))
                UnaryOperations[MyTuple.Create(type, t)] = p => ResolvePrimitive(resolver((T)p.value));
        }

        void AddBiOperation<T, U>(BinaryOperator type, Func<T, U, object> resolver) {
            foreach (var k in GetTypes(typeof(T)).SelectMany(t => GetTypes(typeof(U)), (t, u) => MyTuple.Create(type, t, u)))
                BinaryOperations[k] = (p, q) => ResolvePrimitive(resolver((T)p.value, (U)q.value));
        }


        public void InitializeOperators() {
            //Object
            AddUniOperation<KeyedList>(UnaryOperator.RANDOM, a => a.GetValue(ResolvePrimitive(randomGenerator.Next(a.keyedValues.Count))).GetValue().value);
            AddUniOperation<object>(UnaryOperator.CAST, a => a);
            AddUniOperation<string>(UnaryOperator.CAST, a => {
                Primitive output;
                return Primitive.TryParse(a, out output) ? output.value : a;
            });

            //List
            AddBiOperation<KeyedList, object>(BinaryOperator.ADD, (a, b) => Combine(a, b));
            AddBiOperation<object, KeyedList>(BinaryOperator.ADD, (a, b) => Combine(a, b));
            AddBiOperation<KeyedList, object>(BinaryOperator.SUBTRACT, (a, b) => a.Remove(CastList(ResolvePrimitive(b))));
            AddBiOperation<float, float>(BinaryOperator.RANGE, (a, b) => {
                var range = Range((int)Math.Min(a, b), (int)(Math.Abs(b - a) + 1)).Select(i => GetStaticVariable(i));
                if (a > b) range = range.Reverse();
                return NewKeyedList(range);
            });
            AddBiOperation<string, string>(BinaryOperator.SPLIT, (a, b) => NewKeyedList(a.Split(Once(CastString(ResolvePrimitive(b))).ToArray(), StringSplitOptions.None).Select(GetStaticVariable)));
            AddUniOperation<KeyedList>(UnaryOperator.KEYS, a => a.Keys());
            AddUniOperation<KeyedList>(UnaryOperator.VALUES, a => a.Values());
            AddUniOperation<KeyedList>(UnaryOperator.REVERSE, a => NewKeyedList(a.keyedValues.Select(b => b).Reverse()));
            AddUniOperation<KeyedList>(UnaryOperator.SORT, a => NewKeyedList(a.keyedValues.OrderBy(k => k)));
            AddUniOperation<KeyedList>(UnaryOperator.SHUFFLE, a => NewKeyedList(a.keyedValues.OrderBy(k => randomGenerator.Next())));

            //Booleans
            AddUniOperation<bool>(UnaryOperator.REVERSE, a => !a);
            AddBiOperation<bool, bool>(BinaryOperator.AND, (a, b) => a && b);
            AddBiOperation<bool, bool>(BinaryOperator.OR, (a, b) => a || b);
            AddBiOperation<bool, bool>(BinaryOperator.EXPONENT, (a, b) => a ^ b);
            AddBiOperation<String, object>(BinaryOperator.CONTAINS, (a, b) => a.Contains(CastString(ResolvePrimitive(b))));
            AddBiOperation<KeyedList, object>(BinaryOperator.CONTAINS, (a, b) => CastList(ResolvePrimitive(b)).keyedValues.Select(v => v.GetValue().value).Except(a.keyedValues.Select(v => v.GetValue().value)).Count() == 0);

            //Comparisons
            AddBiOperation<bool, bool>(BinaryOperator.COMPARE, (a, b) => a.CompareTo(b));
            AddBiOperation<string, string>(BinaryOperator.COMPARE, (a, b) => a.CompareTo(b));
            AddBiOperation<float, float>(BinaryOperator.COMPARE, (a, b) => a.CompareTo(b));
            AddBiOperation<Vector3D, Vector3D>(BinaryOperator.COMPARE, (a, b) => !a.Equals(b));
            AddBiOperation<Color, Color>(BinaryOperator.COMPARE, (a, b) => a.PackedValue.CompareTo(b.PackedValue));
            AddBiOperation<Vector3D, float>(BinaryOperator.COMPARE, (a, b) => CastNumber(ResolvePrimitive(a.Length())).CompareTo(b));
            AddBiOperation<float, Vector3D>(BinaryOperator.COMPARE, (a, b) => a.CompareTo(CastNumber(ResolvePrimitive(b.Length()))));
            AddBiOperation<KeyedList, KeyedList>(BinaryOperator.COMPARE, (a, b) => !Enumerable.SequenceEqual(a.keyedValues, b.keyedValues));

            //Numeric
            AddUniOperation<float>(UnaryOperator.REVERSE, a => -a);
            AddUniOperation<float>(UnaryOperator.ABS, a => Math.Abs(a));
            AddUniOperation<float>(UnaryOperator.SQRT, a => Math.Sqrt(a));
            AddUniOperation<float>(UnaryOperator.SIN, a => Math.Sin(a));
            AddUniOperation<float>(UnaryOperator.COS, a => Math.Cos(a));
            AddUniOperation<float>(UnaryOperator.TAN, a => Math.Tan(a));
            AddUniOperation<float>(UnaryOperator.ASIN, a => Math.Asin(a));
            AddUniOperation<float>(UnaryOperator.ACOS, a => Math.Acos(a));
            AddUniOperation<float>(UnaryOperator.ATAN, a => Math.Atan(a));
            AddUniOperation<float>(UnaryOperator.ROUND, a => Math.Round(a));
            AddUniOperation<float>(UnaryOperator.LN, a => Math.Log(a));
            AddUniOperation<Vector3D>(UnaryOperator.ABS, a => a.Length());
            AddUniOperation<Vector3D>(UnaryOperator.SQRT, a => Math.Sqrt(a.Length()));
            AddUniOperation<float>(UnaryOperator.TICKS, a => a / 60);
            AddUniOperation<float>(UnaryOperator.RANDOM, a => randomGenerator.Next((int)a));
            AddUniOperation<float>(UnaryOperator.SIGN, a => Math.Sign(a));
            AddBiOperation<float, float>(BinaryOperator.ADD, (a, b) => a + b);
            AddBiOperation<float, float>(BinaryOperator.SUBTRACT, (a, b) => a - b);
            AddBiOperation<float, float>(BinaryOperator.MULTIPLY, (a, b) => a * b);
            AddBiOperation<float, float>(BinaryOperator.DIVIDE, (a, b) => a / b);
            AddBiOperation<float, float>(BinaryOperator.MOD, (a, b) => a % b);
            AddBiOperation<float, float>(BinaryOperator.EXPONENT, (a, b) => Math.Pow(a, b));
            AddBiOperation<float, float>(BinaryOperator.ROUND, (a, b) => Math.Round(a, (int)b));
            AddBiOperation<Vector3D, Vector3D>(BinaryOperator.DOT, (a, b) => a.Dot(b));
            AddBiOperation<Color, Vector3D>(BinaryOperator.DOT, (a, b) => (a.ToVector3()*255).Dot(b));
            AddBiOperation<Vector3D, Vector3D>(BinaryOperator.EXPONENT, (a, b) => 180 * Math.Acos(a.Dot(b) / (a.Length() * b.Length())) / Math.PI);

            //String
            AddUniOperation<string>(UnaryOperator.REVERSE, a => new string(a.Reverse().ToArray()));
            AddUniOperation<object>(UnaryOperator.TYPE, a => ReturnToString[ResolvePrimitive(a).returnType]);
            AddBiOperation<string, object>(BinaryOperator.ADD, (a, b) => a + CastString(ResolvePrimitive(b)));
            AddBiOperation<object, string>(BinaryOperator.ADD, (a, b) => CastString(ResolvePrimitive(a)) + b);
            AddBiOperation<string, string>(BinaryOperator.SUBTRACT, (a, b) => a.Contains(b) ? a.Remove(a.IndexOf(b)) + a.Substring(a.IndexOf(b) + b.Length) :  a);
            AddBiOperation<string, string>(BinaryOperator.MOD, (a, b) => a.Replace(b, ""));
            AddBiOperation<string, float>(BinaryOperator.SUBTRACT, (a, b) => b >= a.Length ? "" : a.Substring(0, (int)(a.Length - b)));
            AddBiOperation<object, string>(BinaryOperator.CAST, (a, b) => CastMap[b](ResolvePrimitive(a)));
            AddBiOperation<KeyedList, string>(BinaryOperator.JOIN, (a, b) => string.Join(CastString(ResolvePrimitive(b)), a.keyedValues.Select(v => CastString(v.GetValue()))));

            //Vector
            AddUniOperation<Vector3D>(UnaryOperator.SIGN, a => Vector3D.Sign(a));
            AddUniOperation<Vector3D>(UnaryOperator.ROUND, a => Vector3D.Round(a, 0));
            AddUniOperation<Vector3D>(UnaryOperator.REVERSE, a => -a);
            AddBiOperation<Vector3D, Vector3D>(BinaryOperator.ADD, (a,b) => a + b);
            AddBiOperation<Vector3D, Vector3D>(BinaryOperator.SUBTRACT, (a, b) => a - b);
            AddBiOperation<Vector3D, float>(BinaryOperator.ADD, (a, b) => Vector3D.Multiply(a, (a.Length() + b) / a.Length()));
            AddBiOperation<Vector3D, float>(BinaryOperator.SUBTRACT, (a, b) => Vector3D.Multiply(a, (a.Length() - b) / a.Length()));
            AddBiOperation<Vector3D, Vector3D>(BinaryOperator.MULTIPLY, (a, b) => Vector3D.Cross(a, b));
            AddBiOperation<Vector3D, Vector3D>(BinaryOperator.DIVIDE, (a, b) => Vector3D.Divide(a, b.Length()));
            AddBiOperation<Vector3D, float>(BinaryOperator.MULTIPLY, (a, b) => Vector3D.Multiply(a, b));
            AddBiOperation<Vector3D, float>(BinaryOperator.DIVIDE, (a, b) => Vector3D.Divide(a, b));
            AddBiOperation<float, Vector3D>(BinaryOperator.MULTIPLY, (a, b) => Vector3D.Multiply(b, a));
            AddBiOperation<Vector3D, float>(BinaryOperator.ROUND, (a, b) => Vector3D.Round(a, (int)b));

            //Modding a vector by another vector is asking to perform vector rejection.
            //See https://en.wikipedia.org/wiki/Vector_projection
            AddBiOperation<Vector3D, Vector3D>(BinaryOperator.MOD, (a, b) => Vector3D.Reject(a, b));

            //Color
            AddUniOperation<Color>(UnaryOperator.REVERSE, a => new Color(255 - a.R, 255 - a.G, 255 - a.B));
            AddBiOperation<Color, Color>(BinaryOperator.ADD, (a, b) => a + b);
            AddBiOperation<Color, Color>(BinaryOperator.SUBTRACT, (a, b) => new Color(a.R - b.R, a.G - b.G, a.B - b.B));
            AddBiOperation<Color, float>(BinaryOperator.MULTIPLY, (a, b) => Color.Multiply(a, b));
            AddBiOperation<float, Color>(BinaryOperator.MULTIPLY, (a, b) => Color.Multiply(b, a));
            AddBiOperation<Color, float>(BinaryOperator.DIVIDE, (a, b) => Color.Multiply(a, 1/b));
        }

        static KeyedList Combine(object a, object b) => CastList(ResolvePrimitive(a)).Combine(CastList(ResolvePrimitive(b)));
    }
}
