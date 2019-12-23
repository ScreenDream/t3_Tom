﻿using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.TypeColors;

namespace T3.Gui.InputUi
{
    public abstract class InputValueUi<T> : IInputUi
    {
        public static float ConnectionAreaWidth = 30.0f;
        public static float ParameterNameWidth = 120.0f;

        public Symbol.InputDefinition InputDefinition { get; set; }
        public Guid Id => InputDefinition.Id;
        public Relevancy Relevancy { get; set; } = Relevancy.Optional;
        public int Index { get; set; } = 0;
        public virtual bool IsAnimatable => false;

        protected abstract InputEditState DrawEditControl(string name, ref T value);
        protected abstract void DrawValueDisplay(string name, ref T value);

        protected virtual string GetSlotValueAsString(ref T value)
        {
            return String.Empty;
        }

        protected virtual void DrawAnimatedValue(string name, InputSlot<T> inputSlot, Animator animator)
        {
        }
        
        public virtual string GetSlotValue(IInputSlot inputSlot)
        {
            if (inputSlot is InputSlot<T> typedInputSlot)
            {
                return GetSlotValueAsString(ref typedInputSlot.Value);
            }
            return string.Empty;
        }
        
        public InputEditState DrawInputEdit(IInputSlot inputSlot, SymbolUi compositionUi, SymbolChildUi symbolChildUi)
        {
            var name = inputSlot.Input.Name;
            var editState = InputEditState.Nothing;
            var typeColor = TypeUiRegistry.Entries[Type].Color;
            var animator = compositionUi.Symbol.Animator;
            bool isAnimated = IsAnimatable && animator.IsInputSlotAnimated(inputSlot);

            if (inputSlot is InputSlot<T> typedInputSlot)
            {
                var input = inputSlot.Input;
                if (inputSlot.IsConnected)
                {
                    if (typedInputSlot.IsMultiInput)
                    {
                        // Just show actual value
                        ImGui.Button(name + "##paramName", new Vector2(-1, 0));
                        if (ImGui.BeginPopupContextItem("##parameterOptions", 0))
                        {
                            if (ImGui.MenuItem("Parameters settings"))
                                editState = InputEditState.ShowOptions;

                            ImGui.EndPopup();
                        }

                        var multiInput = (MultiInputSlot<T>)typedInputSlot;
                        var allInputs = multiInput.GetCollectedTypedInputs();

                        for (int multiInputIndex = 0; multiInputIndex < allInputs.Count; multiInputIndex++)
                        {
                            ImGui.PushID(multiInputIndex);

                            ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Highlight.Apply(typeColor).Rgba);
                            if (ImGui.Button("->", new Vector2(ConnectionAreaWidth, 0)))
                            {
                                // TODO: implement with proper SelectionManager
                                var compositionSymbol = compositionUi.Symbol;
                                var allConnections = compositionSymbol.Connections.FindAll(c => c.IsTargetOf(symbolChildUi.Id, inputSlot.Id));
                                var connection = allConnections[multiInputIndex];
                                var sourceUi = compositionUi.GetSelectables()
                                                            .First(ui => ui.Id == connection.SourceParentOrChildId || ui.Id == connection.SourceSlotId);
                            }

                            ImGui.PopStyleColor();

                            ImGui.SameLine();

                            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));
                            ImGui.Button("#" + multiInputIndex, new Vector2(ParameterNameWidth, 0.0f));
                            ImGui.PopStyleVar();
                            ImGui.SameLine();

                            ImGui.SetNextItemWidth(-1);
                            ImGui.PushStyleColor(ImGuiCol.Text, T3Style.ConnectedParameterColor.Rgba);
                            var slot = allInputs[multiInputIndex];
                            DrawValueDisplay("##multiInputParam", ref slot.Value);
                            ImGui.PopStyleColor();
                            ImGui.PopID();
                        }

                        ImGui.Spacing();
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Highlight.Apply(typeColor).Rgba);
                        if (ImGui.Button("->", new Vector2(ConnectionAreaWidth, 0.0f)))
                        {
                            // TODO: implement with proper selectionManager
                            var compositionSymbol = compositionUi.Symbol;
                            var con = compositionSymbol.Connections.First(c => c.IsTargetOf(symbolChildUi.Id, inputSlot.Id));
                            var sourceUi = compositionUi.GetSelectables().First(ui => ui.Id == con.SourceParentOrChildId || ui.Id == con.SourceSlotId);
                        }

                        ImGui.PopStyleColor();
                        ImGui.SameLine();

                        // Draw Name
                        ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));
                        ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0.0f));
                        if (ImGui.BeginPopupContextItem("##parameterOptions", 0))
                        {
                            if (ImGui.MenuItem("Parameters settings"))
                                editState = InputEditState.ShowOptions;

                            ImGui.EndPopup();
                        }

                        ImGui.PopStyleVar();

                        ImGui.SameLine();

                        // Draw control
                        ImGui.PushItemWidth(200.0f);
                        ImGui.PushStyleColor(ImGuiCol.Text, input.IsDefault ? Color.Gray.Rgba : Color.White.Rgba);
                        ImGui.SetNextItemWidth(-1);

                        DrawValueDisplay(name, ref typedInputSlot.Value);
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();
                    }
                }
                else if (isAnimated)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, Color.Orange.Rgba);
                    if (ImGui.Button("A", new Vector2(ConnectionAreaWidth, 0.0f)))
                    {
                        animator.RemoveAnimationFrom(inputSlot);
                    }


                    ImGui.PopStyleColor();
                    ImGui.SameLine();

                    // Draw Name
                    ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));
                    ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0.0f));
                    CustomComponents.ContextMenuForItem(() =>
                                                        {
                                                            if (ImGui.MenuItem("Parameters settings"))
                                                                editState = InputEditState.ShowOptions;
                                                        });
                    ImGui.PopStyleVar();
                    ImGui.SameLine();

                    // Draw control
                    ImGui.PushItemWidth(200.0f);
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Orange.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Color.Black.Rgba);
                    ImGui.PushFont(Fonts.FontBold);

                    ImGui.SetNextItemWidth(-1);

                    DrawAnimatedValue(name, typedInputSlot, animator); // todo: command integration

                    ImGui.PopFont();
                    ImGui.PopStyleColor(2);
                    ImGui.PopItemWidth();
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Operator.Apply(typeColor).Rgba);
                    
                    if (ImGui.Button("", new Vector2(ConnectionAreaWidth, 0.0f)))
                    {
                        if (IsAnimatable)
                            animator.CreateInputUpdateAction<float>(inputSlot);
                    }
                    if(ImGui.IsItemHovered()) 
                        ImGui.SetTooltip($"Click to animate\n{input.DefaultValue.ValueType}");

                    ImGui.PopStyleColor();
                    ImGui.SameLine();

                    // Draw Name
                    ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));
                    ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0.0f));
                    CustomComponents.ContextMenuForItem(() =>
                                                        {
                                                            if (ImGui.MenuItem("Set as default", !input.IsDefault))
                                                                input.SetCurrentValueAsDefault();

                                                            if (ImGui.MenuItem("Reset to default", !input.IsDefault))
                                                            {
                                                                input.ResetToDefault();
                                                                foreach (var compositionInstance in compositionUi.Symbol.InstancesOfSymbol)
                                                                {
                                                                    var inputParent = compositionInstance.Children.Single(c => c.SymbolChildId == inputSlot.Parent.SymbolChildId);
                                                                    var slot = inputParent.Inputs.Single(i => i.Id == inputSlot.Id);
                                                                    slot.DirtyFlag.Invalidate(); 
                                                                }
                                                            }

                                                            if (ImGui.MenuItem("Parameters settings"))
                                                                editState = InputEditState.ShowOptions;
                                                        });

                    ImGui.PopStyleVar();

                    ImGui.SameLine();

                    // Draw control
                    ImGui.PushItemWidth(200.0f);
                    ImGui.PushStyleColor(ImGuiCol.Text, input.IsDefault ? Color.Gray.Rgba : Color.White.Rgba);
                    if (input.IsDefault)
                    {
                        // handling default values is a bit tricky with ImGui as we want to show the default
                        // value when this is set, but we never want the default value to be modified. But as
                        // editing is already done when the return value of the ImGui edit control tells us
                        // that editing has happened this here is a simple way to ensure that the default value
                        // is always correct but editing is only happening on the input value.
                        input.Value.Assign(input.DefaultValue);
                    }

                    ImGui.SetNextItemWidth(-1);
                    editState |= DrawEditControl(name, ref typedInputSlot.TypedInputValue.Value);

                    if ((editState & InputEditState.Started) == InputEditState.Started)
                    {
                        //Log.Debug($"focused {name}");
                    }

                    if ((editState & InputEditState.Modified) == InputEditState.Modified)
                    {
                        inputSlot.DirtyFlag.Invalidate();
                    }

                    if ((editState & InputEditState.Finished) == InputEditState.Finished)
                    {
                        inputSlot.DirtyFlag.Invalidate();
                    }

                    if ((editState & InputEditState.ResetToDefault) == InputEditState.ResetToDefault)
                    {
                        input.ResetToDefault();
                        inputSlot.DirtyFlag.Invalidate();
                    }

                    input.IsDefault &= (editState & InputEditState.Modified) != InputEditState.Modified;

                    ImGui.PopStyleColor();
                    ImGui.PopItemWidth();
                }
            }
            else
            {
                Debug.Assert(false);
            }

            return editState;
        }

        public virtual void DrawSettings()
        {
            Type enumType = typeof(Relevancy);
            var values = Enum.GetValues(enumType);
            var valueNames = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                valueNames[i] = Enum.GetName(typeof(Relevancy), values.GetValue(i));
            }

            int index = (int)Relevancy;
            ImGui.Combo("##dropDownRelevancy", ref index, valueNames, valueNames.Length);
            Relevancy = (Relevancy)index;
            ImGui.SameLine();
            ImGui.Text("Relevancy");
        }

        public virtual void Write(JsonTextWriter writer)
        {
            if (Relevancy != DefaultRelevancy)
                writer.WriteObject("Relevancy", Relevancy.ToString());

            var vec2writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];
            writer.WritePropertyName("Position");
            vec2writer(writer, PosOnCanvas);
        }

        public virtual void Read(JToken inputToken)
        {
            Relevancy = (inputToken["Relevancy"] == null)
                            ? DefaultRelevancy
                            : (Relevancy)Enum.Parse(typeof(Relevancy), inputToken["Relevancy"].ToString());

            JToken positionToken = inputToken["Position"];
            PosOnCanvas = new Vector2(positionToken["X"].Value<float>(), positionToken["Y"].Value<float>());
        }

        public Type Type { get; } = typeof(T);
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = SymbolChildUi.DefaultOpSize;
        public bool IsSelected => SelectionManager.IsNodeSelected(this);

        private const Relevancy DefaultRelevancy = Relevancy.Optional;
    }
}