using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public const float RPMToRadiansPerSec = (float)Math.PI / 30f;
        public const float RadiansPerSecToRPM = 30f / (float)Math.PI;

        public static T Type<T>() => default(T);

        public delegate T Supplier<T>();

        //Utilities for constructing collections with few characters
        public static List<T> NewList<T>(params T[] elements) => elements.ToList();
        public static Dictionary<T, U> NewDictionary<T, U>(params KeyValuePair<T, U>[] elements) => elements.ToDictionary(e => e.Key, e => e.Value);
        public static KeyValuePair<T, U> KeyValuePair<T, U>(T key, U value) => new KeyValuePair<T, U>(key, value);

        //Utilities for constructing enumerables
        public static IEnumerable<int> Range(int start, int count) => Enumerable.Range(start, count);
        public static IEnumerable<T> Empty<T>() => Enumerable.Empty<T>();
        public static IEnumerable<T> Once<T>(T element) => Enumerable.Repeat(element, 1);

        //Other useful utilities
        public static IVariable GetStaticVariable(object o) => new StaticVariable(Primitive.From(o));
        public static IVariable EmptyList() => GetStaticVariable(NewKeyedList());
        public static Vector3D Vector(double x, double y, double z) => new Vector3D(x, y, z);
        public static bool AnyNotNull(params Object[] objects) => objects.Any(o => o != null);

        // misc stuff
        static T FindLast<T>(List<IToken> parameters) where T : class, IToken => parameters.OfType<T>().LastOrDefault();
        static T Identitiy<T>(T t) => t;

        // Generate enumerations of words
        static IEnumerable<string> Words(params string[] words) => words;
        static IEnumerable<string> AllWords(params IEnumerable<string>[] words) => words.SelectMany(w => w);
        static IEnumerable<string> PluralWords(params string[] words) => words.Concat(words.Select(w => w + "s"));

        //Conversion stuff
        static string VectorToString(Vector3D vector) => vector.X + ":" + vector.Y + ":" + vector.Z;
        static string ColorToString(Color color) => "#" + IntToHex(color.R) + IntToHex(color.G) + IntToHex(color.B);

        static int HexToInt(string hex) => int.Parse(hex.ToUpper(), System.Globalization.NumberStyles.AllowHexSpecifier);
        static string IntToHex(int hex) => hex.ToString("X2");

        static readonly Dictionary<string, Color> Colors = NewDictionary(
                KeyValuePair("red", Color.Red),
                KeyValuePair("blue", Color.Blue),
                KeyValuePair("green", Color.Green),
                KeyValuePair("orange", Color.Orange),
                KeyValuePair("yellow", Color.Yellow),
                KeyValuePair("white", Color.White),
                KeyValuePair("black", Color.Black)
            );

        public static Color? ParseColor(String s) =>
            (s.StartsWith("#") && s.Length == 7)
            ? new Color(HexToInt(s.Substring(1, 2)), HexToInt(s.Substring(3, 2)), HexToInt(s.Substring(5, 2)))
            : (Colors.ContainsKey(s.ToLower()) ? Colors[s.ToLower()] : (Color?)null);

        public static Vector3D? ParseVector(String s) {
            var components = NewList<double>();
            foreach (var component in s.Split(':')) {
                double result;
                if (Double.TryParse(component, out result)) components.Add(result);
            }
            return components.Count() == 3 ? Vector(components[0], components[1], components[2]) : (Vector3D?)null;
        }
    }
}
