﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Selection;

namespace T3.Gui
{
    public interface IOutputUi : ISelectable
    {
        void DrawValue(Slot slot);
        Type Type { get; }
    }

    public class ValueOutputUi<T> : IOutputUi
    {
        public void DrawValue(Slot slot)
        {
            if (slot is Slot<T> typedSlot)
            {
                var value = typedSlot.GetValue(new EvaluationContext());
                ImGui.Text($"{value}");
            }
            else
            {
                Debug.Assert(false);
            }
        }

        public Type Type { get; } = typeof(T);
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        public bool IsSelected { get; set; }
    }

    public class FloatOutputUi : ValueOutputUi<float>
    {
    }

    public class IntOutputUi : ValueOutputUi<int>
    {
    }

    public class StringOutputUi : ValueOutputUi<string>
    {
    }

    public static class OutputUiRegistry
    {
        public static Dictionary<Guid, Dictionary<Guid, IOutputUi>> Entries { get; } = new Dictionary<Guid, Dictionary<Guid, IOutputUi>>();

        public static void Load()
        {
            if (!File.Exists(FilePath))
            {
                Log.Error($"Couldn't open File '{FilePath} for loading the output ui infos.");
                return;
            }

            var vector2Converter = JsonToTypeValueConverters.Entries[typeof(Vector2)];

            using (var streamReader = new StreamReader(FilePath))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var mainObject = (JObject)JToken.ReadFrom(jsonTextReader);
                var entries = (JArray)mainObject["Entries"];
                foreach (var symbolEntry in entries)
                {
                    var symbolId = Guid.Parse(symbolEntry["SymbolId"].Value<string>());
                    var outputDict = new Dictionary<Guid, IOutputUi>();
                    foreach (var uiOutputEntry in (JArray)symbolEntry["OutputUis"])
                    {
                        var outputId = Guid.Parse(uiOutputEntry["OutputId"].Value<string>());
                        var typeName = uiOutputEntry["Type"].Value<string>();
                        Type type = typeof(float).Assembly.GetTypes().First(t => t.FullName == typeName);
                        if (OutputUiFactory.Entries.TryGetValue(type, out var outputCreator))
                        {
                            var outputUi = outputCreator();
                            JToken positionToken = uiOutputEntry["Position"];
                            outputUi.PosOnCanvas = (Vector2)vector2Converter(positionToken);
                            //JToken sizeToken = uiOutputEntry["Size"];
                            //outputUi.Size = (Vector2)vector2Converter(sizeString);

                            outputDict.Add(outputId, outputUi);
                        }
                        else
                        {
                            Log.Error($"Error creating output ui for non registered type '{typeName}'.");
                        }
                    }
                    Entries.Add(symbolId, outputDict);
                }
            }
        }

        public static void Save()
        {
            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];

            using (var streamWriter = new StreamWriter(FilePath))
            using (var jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                jsonTextWriter.Formatting = Formatting.Indented;
                jsonTextWriter.WriteStartObject(); // root object 
                jsonTextWriter.WritePropertyName("Entries");
                jsonTextWriter.WriteStartArray();

                foreach (var entry in Entries.OrderBy(i => i.Key))
                {
                    var symbol = SymbolRegistry.Entries[entry.Key];
                    jsonTextWriter.WriteStartObject(); // symbol entry
                    jsonTextWriter.WriteValue("SymbolId", entry.Key);
                    jsonTextWriter.WriteComment(symbol.Name);
                    jsonTextWriter.WritePropertyName("OutputUis");
                    jsonTextWriter.WriteStartArray();

                    foreach (var outputEntry in entry.Value.OrderBy(i => i.Key))
                    {
                        jsonTextWriter.WriteStartObject(); // output entry
                        jsonTextWriter.WriteValue("OutputId", outputEntry.Key);
                        var outputUi = outputEntry.Value;
                        var outputName = symbol.OutputDefinitions.Single(outputDef => outputDef.Id == outputEntry.Key).Name;
                        jsonTextWriter.WriteComment(outputName);
                        jsonTextWriter.WriteValue("Type", outputUi.Type);
                        jsonTextWriter.WritePropertyName("Position");
                        vec2Writer(jsonTextWriter, outputUi.PosOnCanvas);

                        //jsonTextWriter.WriteValue("Size", outputUi.Size); //todo: check if needed
                        jsonTextWriter.WriteEndObject();
                    }

                    jsonTextWriter.WriteEndArray();
                    jsonTextWriter.WriteEndObject();
                }

                jsonTextWriter.WriteEndArray();
                jsonTextWriter.WriteEndObject();
            }
        }

        private static string FilePath = "OutputUiRegistry.json";
    }

    public static class OutputUiFactory
    {
        public static Dictionary<Type, Func<IOutputUi>> Entries { get; } = new Dictionary<Type, Func<IOutputUi>>();
    }
}