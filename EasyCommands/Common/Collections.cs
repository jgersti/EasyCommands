﻿using Sandbox.Game.EntityComponents;
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
        public class KeyedList {
            List<KeyedVariable> keyedValues;

            public KeyedList(params Variable[] values) {
                keyedValues = values.ToList().ConvertAll(AsKeyedVariable);
            }

            public List<Variable> GetValues() => keyedValues.ConvertAll(v => (Variable)v);

            //If numeric, get by index.  If string, get by key value
            public Variable GetValue(Primitive key) {
                switch(key.GetPrimitiveType()) {
                    case Return.NUMERIC:
                        return keyedValues[(int)CastNumber(key).GetTypedValue()];
                    case Return.STRING:
                        var keyString = CastString(key).GetTypedValue();
                        return keyedValues.Where(v => v.Key == CastString(key).GetTypedValue())
                            .Cast<Variable>()
                            .DefaultIfEmpty(EmptyList())
                            .First();
                    default:
                        throw new Exception("Cannot lookup collection value by Primitive Type: " + key.GetPrimitiveType());
                }
            }

            //If numeric, set by Index.  If string, put (or append) keyed value
            public void SetValue(Primitive key, Variable value) {
                if (key.GetPrimitiveType() == Return.NUMERIC) {
                    keyedValues[(int)CastNumber(key).GetTypedValue()] = AsKeyedVariable(value);
                } else if (key.GetPrimitiveType() == Return.STRING) {
                    var keyString = CastString(key).GetTypedValue();
                    KeyedVariable existing = keyedValues.Where(v => v.Key == keyString).FirstOrDefault();
                    if (existing == null) {
                        keyedValues.Add(new KeyedVariable(keyString, value));
                    } else existing.Value = value;
                } else throw new Exception("Cannot set collection value by Primitive Type: " + key.GetPrimitiveType());
            }

            //TODO: Merge and then de-dupe keyed values (last wins?)
            public KeyedList Combine(KeyedList other) {
                return new KeyedList(GetValues().Concat(other.GetValues()).ToArray());
            }

            public KeyedList Keys() {
                return new KeyedList(keyedValues.Where(v => !string.IsNullOrEmpty(v.Key)).Select(v => GetStaticVariable(v.Key)).ToArray());
            }

            public String Print() {
                return "[" + string.Join(",", keyedValues.Select(k => (string.IsNullOrEmpty(k.Key) ? "" : k.Key + "=") + CastString(k.Value.GetValue()).GetTypedValue())) + "]";
            }
        }
    }
}
