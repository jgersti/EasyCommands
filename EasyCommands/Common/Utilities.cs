﻿using Sandbox.Game.EntityComponents;
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
        public const float RPMToRadiansPerSec = (float)(Math.PI / 30);
        public const float RadiansPerSecToRPM = (float)(30 / Math.PI);
        public const float RadiansToDegrees = (float)(180 / Math.PI);
        public const float DegreesToRadians = (float)(Math.PI / 180);

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
        public static IVariable GetStaticVariable(object o) => new StaticVariable(ResolvePrimitive(o));
        public static IVariable EmptyList() => GetStaticVariable(NewKeyedList());
        public static Vector3D Vector(double x, double y, double z) => new Vector3D(x, y, z);
        public static bool AnyNotNull(params Object[] objects) => objects.Any(o => o != null);
    }
}
