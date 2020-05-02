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

namespace IngameScript
{
    partial class Program
    {
        public class ProgramBlockHandler : BlockHandler<IMyProgrammableBlock>
        {
            public ProgramBlockHandler() : base()
            {
                booleanPropertyGetters.Add(BooleanPropertyType.RUNNING, block => block.DetailedInfo.EndsWith("Running\n"));
                booleanPropertyGetters.Add(BooleanPropertyType.STOPPED, block => block.DetailedInfo.EndsWith("Stopped\n"));
                booleanPropertyGetters.Add(BooleanPropertyType.PAUSED, block => block.DetailedInfo.EndsWith("Paused\n"));
                booleanPropertyGetters.Add(BooleanPropertyType.COMPLETE, block => block.DetailedInfo.EndsWith("Complete\n"));
                stringPropertySetters.Add(StringPropertyType.RUN, (block, value) => block.TryRun(value));
                defaultStringProperty = StringPropertyType.RUN;
            }
        }
    }
}