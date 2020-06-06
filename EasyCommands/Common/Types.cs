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
        public enum BlockType { PISTON, ROTOR, PROGRAM, TIMER, LIGHT, PROJECTOR, MERGE, CONNECTOR, WELDER, GRINDER, DOOR }
        public enum BooleanPropertyType { ON_OFF, CONNECTED, CONNECTABLE, ANGLE, LOCKED, LOCKABLE, RUNNING, PAUSED, STOPPED, COMPLETE, OPEN }
        public enum NumericPropertyType { HEIGHT, ANGLE, VELOCITY, PROGRESS, CLOSED_RATIO }
        public enum StringPropertyType { NAME, RUN }
        public enum UnitType { SECONDS, TICKS, DEGREES, RADIANS, METERS, RPM }
        public enum DirectionType { UP, DOWN, CLOCKWISE, COUNTERCLOCKWISE }
        public enum ComparisonType { GREATER, GREATER_OR_EQUAL, EQUAL, LESS_OR_EQUAL, LESS }
        public enum ControlType { START, RESTART, STOP, PARSE, LOOP, PAUSE, RESUME }
        public enum ProgramState { RUNNING, STOPPED, COMPLETE, PAUSED }
        public enum FunctionType { GOTO, GOSUB }
    }
}