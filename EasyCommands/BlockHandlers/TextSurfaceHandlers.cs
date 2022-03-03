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
        public class TextSurfaceHandler : MultiInstanceBlockHandler<IMyTextSurface> {
            public TextSurfaceHandler() {
                var fontSizeHandler = NumericHandler(b => b.FontSize, (b, v) => b.FontSize = v, 0.5f);
                var fontColorHandler = ColorHandler(b => b.ContentType == ContentType.SCRIPT ? b.ScriptForegroundColor : b.FontColor, (b, v) => { if (b.ContentType == ContentType.SCRIPT) b.ScriptForegroundColor = v; else b.FontColor = v; });

                AddStringHandler(Property.NAME, b => Name(b));
                AddBooleanHandler(Property.ENABLE, b => b.ContentType != ContentType.NONE, (b, v) => b.ContentType = v ? ContentType.TEXT_AND_IMAGE : ContentType.NONE);
                AddStringHandler(Property.TEXT, b => { var builder = new StringBuilder(); b.ReadText(builder); return builder.ToString(); }, (b, v) => { b.ContentType = ContentType.TEXT_AND_IMAGE; b.WriteText(v); });
                AddStringHandler(Property.MEDIA, b => b.CurrentlyShownImage ?? "", (b,v) => SetImages(b, Primitive.From(v).AsList()));
                AddStringHandler(Property.RUN, b => b.Script ?? "", (b, v) => { b.ContentType = ContentType.SCRIPT; b.Script = v; });
                AddNumericHandler(Property.OFFSET, b => b.TextPadding, (b, v) => b.TextPadding = v, 1);
                AddBooleanHandler(Property.RATIO, b => b.PreserveAspectRatio, (b, v) => b.PreserveAspectRatio = v);
                AddListHandler(Property.MEDIA_LIST, b => {
                    var images = NewList<string>();
                    b.GetSelectedImages(images);
                    return NewKeyedList(images.Select(GetStaticVariable));
                }, SetImages);
                AddStringHandler(Property.POSITION, b => (b.Alignment + "").ToLower(), (b, v) => b.Alignment = v == "center" ? TextAlignment.CENTER : v == "right" ? TextAlignment.RIGHT : TextAlignment.LEFT);
                AddNumericHandler(Property.INTERVAL, b => b.ChangeInterval, (b, v) => b.ChangeInterval = v, 1);
                AddPropertyHandler(Property.LEVEL, fontSizeHandler);
                AddPropertyHandler(Property.COLOR, fontColorHandler);
                AddColorHandler(Property.BACKGROUND, b => b.ContentType == ContentType.SCRIPT ? b.ScriptBackgroundColor : b.BackgroundColor, (b, v) => { if (b.ContentType == ContentType.SCRIPT) b.ScriptBackgroundColor = v; else b.BackgroundColor = v; });
                AddReturnHandlers(Property.FONT, Return.STRING,
                    TypeHandler(StringHandler(b => b.Font, (b,v) => b.Font = v), Return.STRING),
                    TypeHandler(fontSizeHandler, Return.NUMERIC),
                    TypeHandler(fontColorHandler, Return.COLOR));

                defaultPropertiesByPrimitive[Return.BOOLEAN] = Property.ENABLE;
                defaultPropertiesByPrimitive[Return.STRING] = Property.TEXT;
                defaultPropertiesByPrimitive[Return.COLOR] = Property.COLOR;
                defaultPropertiesByPrimitive[Return.NUMERIC] = Property.LEVEL;
                defaultPropertiesByPrimitive[Return.LIST] = Property.MEDIA_LIST;
                defaultPropertiesByDirection[Direction.UP] = Property.TEXT;
            }

            void SetImages(IMyTextSurface block, KeyedList images) {
                block.ContentType = ContentType.TEXT_AND_IMAGE;
                block.ClearImagesFromSelection();
                block.AddImagesToSelection(images.keyedValues.Select(i => i.GetValue().AsString()).ToList());
            }

            public override string Name(IMyTextSurface block) => block.DisplayName;

            public override IEnumerable<IMyTextSurface> GetInstances(IMyTerminalBlock block) =>
                block is IMyTextSurface
                    ? Once((IMyTextSurface)block)
                    : block is IMyTextSurfaceProvider
                        ? Range(0, ((IMyTextSurfaceProvider)block).SurfaceCount).Select(((IMyTextSurfaceProvider)block).GetSurface)
                        : Empty<IMyTextSurface>();
        }
    }
}
