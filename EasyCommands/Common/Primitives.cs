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
        public class Primitive : IComparable<Primitive> {
            public Return returnType;
            public object value;

            Primitive(Return t, object v) {
                returnType = t;
                value = v;
            }

            public Primitive Plus(Primitive p) => Operations.Evaluate(BinaryOperator.ADD, this, p);
            public Primitive Minus(Primitive p) => Operations.Evaluate(BinaryOperator.SUBTRACT, this, p);
            public Primitive Multiply(Primitive p) => Operations.Evaluate(BinaryOperator.MULTIPLY, this, p);
            public Primitive Divide(Primitive p) => Operations.Evaluate(BinaryOperator.DIVIDE, this, p);
            public int CompareTo(Primitive p) => Convert.ToInt32(Operations.Evaluate(BinaryOperator.COMPARE, this, p).AsNumber());
            public Primitive Not() => Operations.Evaluate(UnaryOperator.REVERSE, this);
            public Primitive DeepCopy() => From((value as KeyedList)?.DeepCopy() ?? value);

            public T As<T>() => (T)CastFunctions[typeof(T)].GetValueOrDefault(returnType, CastFunctions[typeof(T)][Return.DEFAULT])(this);
            public bool AsBool() => As<bool>();
            public float AsNumber() => As<float>();
            public string AsString() => As<string>().Replace("\\n", "\n");
            public Vector3D AsVector() => As<Vector3D>();
            public Color AsColor() => As<Color>();
            public KeyedList AsList() => As<KeyedList>();

            public static bool TryParse(String value, out Primitive primitive) {
                primitive = null;
                bool boolean;
                double numeric;
                var vector = ParseVector(value);
                var color = ParseColor(value);
                if (bool.TryParse(value, out boolean)) primitive = From(boolean);
                if (double.TryParse(value, out numeric)) primitive = From(numeric);
                if (vector.HasValue) primitive = From(vector.Value);
                if (color.HasValue) primitive = From(color.Value);
                return primitive != null;
            }
            public static Primitive From(object o) => new Primitive(PrimitiveTypeMap[o.GetType()], (o is double || o is int) ? Convert.ToSingle(o) : o);

            public delegate object Converter(Primitive p);
            public static KeyValuePair<T, Converter> CastFunction<T>(T r, Converter func) => KeyValuePair(r, func);
            static Converter Failure(Return returnType) => p => { throw new Exception("Cannot convert " + ReturnToString[p.returnType] + " " + p.AsString() + " to " + ReturnToString[returnType]); };
            static readonly Dictionary<Type, Dictionary<Return, Converter>> CastFunctions = NewDictionary(
                KeyValuePair(typeof(bool), NewDictionary(
                    CastFunction(Return.BOOLEAN, p => p.value),
                    CastFunction(Return.NUMERIC, p => p.AsNumber() != 0),
                    CastFunction(Return.STRING, p => {
                        Primitive primitive;
                        return Primitive.TryParse(p.AsString(), out primitive) && primitive.AsBool();
                    }),
                    CastFunction(Return.DEFAULT, Failure(Return.BOOLEAN))
                )),
                KeyValuePair(typeof(float), NewDictionary(
                    CastFunction(Return.BOOLEAN, p => p.AsBool() ? 1.0f : 0.0f),
                    CastFunction(Return.NUMERIC, p => p.value),
                    CastFunction(Return.STRING, p => float.Parse(p.AsString())),
                    CastFunction(Return.VECTOR, p => (float)p.AsVector().Length()),
                    CastFunction(Return.DEFAULT, Failure(Return.NUMERIC))
                )),
                KeyValuePair(typeof(string), NewDictionary(
                    CastFunction(Return.VECTOR, p => VectorToString(p.AsVector())),
                    CastFunction(Return.COLOR, p => ColorToString(p.AsColor())),
                    CastFunction(Return.LIST, p => p.AsList().Print()),
                    CastFunction(Return.DEFAULT, p => "" + p.value)

                )),
                KeyValuePair(typeof(Vector3D), NewDictionary(
                    CastFunction(Return.STRING, p => ParseVector(p.AsString()).Value),
                    CastFunction(Return.VECTOR, p => p.value),
                    CastFunction(Return.COLOR, p => Vector(p.AsColor().R, p.AsColor().G, p.AsColor().B)),
                    CastFunction(Return.DEFAULT, Failure(Return.VECTOR))
                )),
                KeyValuePair(typeof(Color), NewDictionary(
                    CastFunction(Return.NUMERIC, p => new Color(p.AsNumber())),
                    CastFunction(Return.STRING, p => ParseColor(p.AsString()).Value),
                    CastFunction(Return.VECTOR, p => new Color((int)p.AsVector().X, (int)p.AsVector().Y, (int)p.AsVector().Z)),
                    CastFunction(Return.COLOR, p => p.value),
                    CastFunction(Return.DEFAULT, Failure(Return.COLOR))
                )),
                KeyValuePair(typeof(KeyedList), NewDictionary(
                    CastFunction(Return.LIST, p => p.value),
                    CastFunction(Return.DEFAULT, p => NewKeyedList(Once(GetStaticVariable(p.value))))
                ))
            );
        }

        static Dictionary<Type, Return> PrimitiveTypeMap = NewDictionary(
            KeyValuePair(typeof(bool), Return.BOOLEAN),
            KeyValuePair(typeof(string), Return.STRING),
            KeyValuePair(typeof(float), Return.NUMERIC),
            KeyValuePair(typeof(int), Return.NUMERIC),
            KeyValuePair(typeof(double), Return.NUMERIC),
            KeyValuePair(typeof(Vector3D), Return.VECTOR),
            KeyValuePair(typeof(Color), Return.COLOR),
            KeyValuePair(typeof(KeyedList), Return.LIST)
        );

        public static List<Return> GetTypes(Type type) =>
            type != typeof(object)
            ? NewList(PrimitiveTypeMap[type])
            : NewList((Return[])Enum.GetValues(typeof(Return)));
    }
}
