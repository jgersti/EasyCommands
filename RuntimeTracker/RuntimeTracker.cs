﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        /// <summary>
        /// Whiplash141's RuntimeTracker
        /// <see cref="href=https://github.com/Whiplash141/SpaceEngineersScripts/blob/master/Unpolished/RuntimeTracker.cs">Github link</see>
        /// </summary>
        /// <example><code>
        /// runtimeTracker _tracker;
        /// Program() {
        ///     _tracker = new RuntimeTracker(this);
        /// }
        ///
        /// void Main() {
        ///     _tracker.AddRuntime();
        ///     // Do stuff
        ///
        ///     // At the end of main:
        ///     _tracker.AddInstructions();
        ///
        ///     Echo(_tracker.Write());
        /// }
        /// </code></example>
        public class RuntimeTracker {
            public int Capacity { get; set; }
            public double Sensitivity { get; set; }
            public double MaxRuntime { get; private set; }
            public double MaxInstructions { get; private set; }
            public double AverageRuntime { get; private set; }
            public double AverageInstructions { get; private set; }
            public double LastRuntime { get; private set; }
            public double LastInstructions { get; private set; }

            readonly Queue<double> _runtimes = new Queue<double>();
            readonly Queue<double> _instructions = new Queue<double>();
            readonly int _instructionLimit;
            readonly Program _program;
            const double MS_PER_TICK = 16.6666;

            const string Format = "General Runtime Info\n"
            + "- Avg runtime: {0:n4} ms\n"
            + "- Last runtime: {1:n4} ms\n"
            + "- Max runtime: {2:n4} ms\n"
            + "- Avg instructions: {3:n2}\n"
            + "- Last instructions: {4:n0}\n"
            + "- Max instructions: {5:n0}\n"
            + "- Avg complexity: {6:0.000}%";

            public RuntimeTracker(Program program, int capacity = 100, double sensitivity = 0.005) {
                _program = program;
                Capacity = capacity;
                Sensitivity = sensitivity;
                _instructionLimit = _program.Runtime.MaxInstructionCount;
            }

            public void AddRuntime() {
                double runtime = _program.Runtime.LastRunTimeMs;
                LastRuntime = runtime;
                AverageRuntime += (Sensitivity * runtime);
                int roundedTicksSinceLastRuntime = (int)Math.Round(_program.Runtime.TimeSinceLastRun.TotalMilliseconds / MS_PER_TICK);
                if (roundedTicksSinceLastRuntime == 1) {
                    AverageRuntime *= (1 - Sensitivity);
                }
                else if (roundedTicksSinceLastRuntime > 1) {
                    AverageRuntime *= Math.Pow((1 - Sensitivity), roundedTicksSinceLastRuntime);
                }

                _runtimes.Enqueue(runtime);
                if (_runtimes.Count == Capacity) {
                    _runtimes.Dequeue();
                }

                MaxRuntime = _runtimes.Max();
            }

            public void AddInstructions() {
                double instructions = _program.Runtime.CurrentInstructionCount;
                LastInstructions = instructions;
                AverageInstructions = Sensitivity * (instructions - AverageInstructions) + AverageInstructions;

                _instructions.Enqueue(instructions);
                if (_instructions.Count == Capacity) {
                    _instructions.Dequeue();
                }

                MaxInstructions = _instructions.Max();
            }

            public string Write() {
                return string.Format(
                    Format,
                    AverageRuntime,
                    LastRuntime,
                    MaxRuntime,
                    AverageInstructions,
                    LastInstructions,
                    MaxInstructions,
                    AverageInstructions / _instructionLimit);
            }
        }
    }
}
