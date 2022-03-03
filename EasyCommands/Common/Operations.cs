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
        public static class Operations {
            delegate Primitive UnaryOperation(Primitive a);
            delegate Primitive BinaryOperation(Primitive a, Primitive b);

            static readonly Dictionary<MyTuple<UnaryOperator, Return>, UnaryOperation> UnaryOperations;
            static readonly Dictionary<MyTuple<BinaryOperator, Return, Return>, BinaryOperation> BinaryOperations;

            static readonly Dictionary<string, Primitive.Converter> CastMap = NewDictionary(
                Primitive.CastFunction("bool", p => p.AsBool()),
                Primitive.CastFunction("boolean", p => p.AsBool()),
                Primitive.CastFunction("string", p => p.AsString()),
                Primitive.CastFunction("number", p => p.AsNumber()),
                Primitive.CastFunction("vector", p => p.AsVector()),
                Primitive.CastFunction("color", p => p.AsColor()),
                Primitive.CastFunction("list", p => p.AsList())
            );

            static Operations() {
                UnaryOperations = NewDictionary<MyTuple<UnaryOperator, Return>, UnaryOperation>();
                BinaryOperations = NewDictionary<MyTuple<BinaryOperator, Return, Return>, BinaryOperation>();

                //Object
                AddUnaryOperation<KeyedList>(UnaryOperator.RANDOM, a => a.GetValue(Primitive.From(PROGRAM.randomGenerator.Next(a.keyedValues.Count))).GetValue().value);
                AddUnaryOperation<object>(UnaryOperator.CAST, a => a);
                AddUnaryOperation<string>(UnaryOperator.CAST, a => {
                    Primitive output;
                    return Primitive.TryParse(a, out output) ? output.value : a;
                });

                //List
                AddBinaryOperation<KeyedList, object>(BinaryOperator.ADD, (a, b) => Combine(a, b));
                AddBinaryOperation<object, KeyedList>(BinaryOperator.ADD, (a, b) => Combine(a, b));
                AddBinaryOperation<KeyedList, object>(BinaryOperator.SUBTRACT, (a, b) => a.Remove(Primitive.From(b).AsList()));
                AddBinaryOperation<float, float>(BinaryOperator.RANGE, (a, b) => {
                    var range = Range((int)Math.Min(a, b), (int)(Math.Abs(b - a) + 1)).Select(i => GetStaticVariable(i));
                    if (a > b) range = range.Reverse();
                    return NewKeyedList(range);
                });
                AddBinaryOperation<string, string>(BinaryOperator.SPLIT, (a, b) => NewKeyedList(a.Split(Once(Primitive.From(b).AsString()).ToArray(), StringSplitOptions.None).Select(GetStaticVariable)));
                AddUnaryOperation<KeyedList>(UnaryOperator.KEYS, a => a.Keys());
                AddUnaryOperation<KeyedList>(UnaryOperator.VALUES, a => a.Values());
                AddUnaryOperation<KeyedList>(UnaryOperator.REVERSE, a => NewKeyedList(a.keyedValues.Select(b => b).Reverse()));
                AddUnaryOperation<KeyedList>(UnaryOperator.SORT, a => NewKeyedList(a.keyedValues.OrderBy(k => k)));
                AddUnaryOperation<KeyedList>(UnaryOperator.SHUFFLE, a => NewKeyedList(a.keyedValues.OrderBy(k => PROGRAM.randomGenerator.Next())));

                //Booleans
                AddUnaryOperation<bool>(UnaryOperator.REVERSE, a => !a);
                AddBinaryOperation<bool, bool>(BinaryOperator.AND, (a, b) => a && b);
                AddBinaryOperation<bool, bool>(BinaryOperator.OR, (a, b) => a || b);
                AddBinaryOperation<bool, bool>(BinaryOperator.EXPONENT, (a, b) => a ^ b);
                AddBinaryOperation<String, object>(BinaryOperator.CONTAINS, (a, b) => a.Contains(Primitive.From(b).AsString()));
                AddBinaryOperation<KeyedList, object>(BinaryOperator.CONTAINS, (a, b) => Primitive.From(b).AsList().keyedValues.Select(v => v.GetValue().value).Except(a.keyedValues.Select(v => v.GetValue().value)).Count() == 0);

                //Comparisons
                AddBinaryOperation<bool, bool>(BinaryOperator.COMPARE, (a, b) => a.CompareTo(b));
                AddBinaryOperation<string, string>(BinaryOperator.COMPARE, (a, b) => a.CompareTo(b));
                AddBinaryOperation<float, float>(BinaryOperator.COMPARE, (a, b) => a.CompareTo(b));
                AddBinaryOperation<Vector3D, Vector3D>(BinaryOperator.COMPARE, (a, b) => !a.Equals(b));
                AddBinaryOperation<Color, Color>(BinaryOperator.COMPARE, (a, b) => a.PackedValue.CompareTo(b.PackedValue));
                AddBinaryOperation<Vector3D, float>(BinaryOperator.COMPARE, (a, b) => Primitive.From(a.Length()).AsNumber().CompareTo(b)); // ***
                AddBinaryOperation<float, Vector3D>(BinaryOperator.COMPARE, (a, b) => a.CompareTo(Primitive.From(b.Length()).AsNumber())); // ***
                AddBinaryOperation<KeyedList, KeyedList>(BinaryOperator.COMPARE, (a, b) => !Enumerable.SequenceEqual(a.keyedValues, b.keyedValues));

                //Numeric
                AddUnaryOperation<float>(UnaryOperator.REVERSE, a => -a);
                AddUnaryOperation<float>(UnaryOperator.ABS, a => Math.Abs(a));
                AddUnaryOperation<float>(UnaryOperator.SQRT, a => Math.Sqrt(a));
                AddUnaryOperation<float>(UnaryOperator.SIN, a => Math.Sin(a));
                AddUnaryOperation<float>(UnaryOperator.COS, a => Math.Cos(a));
                AddUnaryOperation<float>(UnaryOperator.TAN, a => Math.Tan(a));
                AddUnaryOperation<float>(UnaryOperator.ASIN, a => Math.Asin(a));
                AddUnaryOperation<float>(UnaryOperator.ACOS, a => Math.Acos(a));
                AddUnaryOperation<float>(UnaryOperator.ATAN, a => Math.Atan(a));
                AddUnaryOperation<float>(UnaryOperator.ROUND, a => Math.Round(a));
                AddUnaryOperation<float>(UnaryOperator.LN, a => Math.Log(a));
                AddUnaryOperation<Vector3D>(UnaryOperator.ABS, a => a.Length());
                AddUnaryOperation<Vector3D>(UnaryOperator.SQRT, a => Math.Sqrt(a.Length()));
                AddUnaryOperation<float>(UnaryOperator.TICKS, a => a / 60);
                AddUnaryOperation<float>(UnaryOperator.RANDOM, a => PROGRAM.randomGenerator.Next((int)a));
                AddUnaryOperation<float>(UnaryOperator.SIGN, a => Math.Sign(a));
                AddBinaryOperation<float, float>(BinaryOperator.ADD, (a, b) => a + b);
                AddBinaryOperation<float, float>(BinaryOperator.SUBTRACT, (a, b) => a - b);
                AddBinaryOperation<float, float>(BinaryOperator.MULTIPLY, (a, b) => a * b);
                AddBinaryOperation<float, float>(BinaryOperator.DIVIDE, (a, b) => a / b);
                AddBinaryOperation<float, float>(BinaryOperator.MOD, (a, b) => a % b);
                AddBinaryOperation<float, float>(BinaryOperator.EXPONENT, (a, b) => Math.Pow(a, b));
                AddBinaryOperation<float, float>(BinaryOperator.ROUND, (a, b) => Math.Round(a, (int)b));
                AddBinaryOperation<Vector3D, Vector3D>(BinaryOperator.DOT, (a, b) => a.Dot(b));
                AddBinaryOperation<Color, Vector3D>(BinaryOperator.DOT, (a, b) => (a.ToVector3() * 255).Dot(b));
                AddBinaryOperation<Vector3D, Vector3D>(BinaryOperator.EXPONENT, (a, b) => 180 * Math.Acos(a.Dot(b) / (a.Length() * b.Length())) / Math.PI);

                //String
                AddUnaryOperation<string>(UnaryOperator.REVERSE, a => new string(a.Reverse().ToArray()));
                AddUnaryOperation<object>(UnaryOperator.TYPE, a => ReturnToString[Primitive.From(a).returnType]);
                AddBinaryOperation<string, object>(BinaryOperator.ADD, (a, b) => a + Primitive.From(b).AsString());
                AddBinaryOperation<object, string>(BinaryOperator.ADD, (a, b) => Primitive.From(a).AsString() + b);
                AddBinaryOperation<string, string>(BinaryOperator.SUBTRACT, (a, b) => a.Contains(b) ? a.Remove(a.IndexOf(b)) + a.Substring(a.IndexOf(b) + b.Length) : a);
                AddBinaryOperation<string, string>(BinaryOperator.MOD, (a, b) => a.Replace(b, ""));
                AddBinaryOperation<string, float>(BinaryOperator.SUBTRACT, (a, b) => b >= a.Length ? "" : a.Substring(0, (int)(a.Length - b)));
                AddBinaryOperation<object, string>(BinaryOperator.CAST, (a, b) => CastMap[b](Primitive.From(a)));
                AddBinaryOperation<KeyedList, string>(BinaryOperator.JOIN, (a, b) => string.Join(Primitive.From(b).AsString(), a.keyedValues.Select(v => v.GetValue().AsString())));

                //Vector
                AddUnaryOperation<Vector3D>(UnaryOperator.SIGN, a => Vector3D.Sign(a));
                AddUnaryOperation<Vector3D>(UnaryOperator.ROUND, a => Vector3D.Round(a, 0));
                AddUnaryOperation<Vector3D>(UnaryOperator.REVERSE, a => -a);
                AddBinaryOperation<Vector3D, Vector3D>(BinaryOperator.ADD, (a, b) => a + b);
                AddBinaryOperation<Vector3D, Vector3D>(BinaryOperator.SUBTRACT, (a, b) => a - b);
                AddBinaryOperation<Vector3D, float>(BinaryOperator.ADD, (a, b) => Vector3D.Multiply(a, (a.Length() + b) / a.Length()));
                AddBinaryOperation<Vector3D, float>(BinaryOperator.SUBTRACT, (a, b) => Vector3D.Multiply(a, (a.Length() - b) / a.Length()));
                AddBinaryOperation<Vector3D, Vector3D>(BinaryOperator.MULTIPLY, (a, b) => Vector3D.Cross(a, b));
                AddBinaryOperation<Vector3D, Vector3D>(BinaryOperator.DIVIDE, (a, b) => Vector3D.Divide(a, b.Length()));
                AddBinaryOperation<Vector3D, float>(BinaryOperator.MULTIPLY, (a, b) => Vector3D.Multiply(a, b));
                AddBinaryOperation<Vector3D, float>(BinaryOperator.DIVIDE, (a, b) => Vector3D.Divide(a, b));
                AddBinaryOperation<float, Vector3D>(BinaryOperator.MULTIPLY, (a, b) => Vector3D.Multiply(b, a));
                AddBinaryOperation<Vector3D, float>(BinaryOperator.ROUND, (a, b) => Vector3D.Round(a, (int)b));

                //Modding a vector by another vector is asking to perform vector rejection.
                //See https://en.wikipedia.org/wiki/Vector_projection
                AddBinaryOperation<Vector3D, Vector3D>(BinaryOperator.MOD, (a, b) => Vector3D.Reject(a, b));

                //Color
                AddUnaryOperation<Color>(UnaryOperator.REVERSE, a => new Color(255 - a.R, 255 - a.G, 255 - a.B));
                AddBinaryOperation<Color, Color>(BinaryOperator.ADD, (a, b) => a + b);
                AddBinaryOperation<Color, Color>(BinaryOperator.SUBTRACT, (a, b) => new Color(a.R - b.R, a.G - b.G, a.B - b.B));
                AddBinaryOperation<Color, float>(BinaryOperator.MULTIPLY, (a, b) => Color.Multiply(a, b));
                AddBinaryOperation<float, Color>(BinaryOperator.MULTIPLY, (a, b) => Color.Multiply(b, a));
                AddBinaryOperation<Color, float>(BinaryOperator.DIVIDE, (a, b) => Color.Multiply(a, 1 / b));
            }

            static void AddUnaryOperation<T>(UnaryOperator type, Func<T, object> resolver) {
                foreach (Return t in GetTypes(typeof(T)))
                    UnaryOperations[MyTuple.Create(type, t)] = p => Primitive.From(resolver((T)p.value));
            }

            static void AddBinaryOperation<T, U>(BinaryOperator type, Func<T, U, object> resolver) {
                foreach (var k in GetTypes(typeof(T)).SelectMany(t => GetTypes(typeof(U)), (t, u) => MyTuple.Create(type, t, u)))
                    BinaryOperations[k] = (p, q) => Primitive.From(resolver((T)p.value, (U)q.value));
            }

            public static Primitive Evaluate(UnaryOperator type, Primitive a) =>
                UnaryOperations.GetValueOrDefault(MyTuple.Create(type, a.returnType), p => {
                    throw new Exception("Cannot perform operation: " + UnaryOperatorToString[type] + " on type: " + ReturnToString[p.returnType]);
                })(a);

            public static Primitive Evaluate(BinaryOperator type, Primitive a, Primitive b) =>
                BinaryOperations.GetValueOrDefault(MyTuple.Create(type, a.returnType, b.returnType), (p, q) => {
                    throw new Exception("Cannot perform operation: " + BinaryOperatorToString[type] + " on types: " + ReturnToString[p.returnType] + ", " + ReturnToString[q.returnType]);
                })(a, b);

            static KeyedList Combine(object a, object b) => Primitive.From(a).AsList().Combine(Primitive.From(b).AsList());
        }
    }
}
