﻿using ImGuiNET;
using imHelpers;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    public static class Slots
    {
        public static void DrawOutputSlot(SymbolChildUi ui, int outputIndex)
        {
            var outputDef = ui.SymbolChild.Symbol.OutputDefinitions[outputIndex];

            var virtualRectInCanvas = GetOutputSlotSizeInCanvas(ui, outputIndex);

            var rInScreen = GraphCanvas.Current.TransformRect(virtualRectInCanvas);

            ImGui.SetCursorScreenPos(rInScreen.Min);
            ImGui.PushID(ui.SymbolChild.Id.GetHashCode());

            ImGui.InvisibleButton("output", rInScreen.GetSize());
            THelpers.DebugItemRect();
            var color = ColorForType(outputDef);

            //Note: isItemHovered will not work
            var hovered = DraftConnection.TempConnection != null ? rInScreen.Contains(ImGui.GetMousePos())
                : ImGui.IsItemHovered();

            if (DraftConnection.IsOutputSlotCurrentConnectionSource(ui, outputIndex))
            {
                GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, ColorForType(outputDef));

                if (ImGui.IsMouseDragging(0))
                {
                    DraftConnection.Update();
                }
            }
            else if (hovered)
            {
                if (DraftConnection.IsMatchingOutputType(outputDef.ValueType))
                {
                    GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                    if (ImGui.IsMouseReleased(0))
                    {
                        DraftConnection.CompleteAtOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, ui, outputIndex);
                    }
                }
                else
                {
                    GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, Color.White);
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($".{outputDef.Name} ->");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        DraftConnection.StartFromOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, ui, outputIndex);
                    }
                    //ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    //ImGui.PushStyleColor(ImGuiCol.PopupBg, new Color(0.2f).Rgba);
                    //ImGui.BeginTooltip();
                    //ImGui.Text($"-> .{outputDef.Name}");
                    //ImGui.EndTooltip();
                    //ImGui.PopStyleColor();
                    //ImGui.PopStyleVar();
                }
            }
            else
            {
                GraphCanvas.Current.DrawRectFilled(
                    ImRect.RectWithSize(
                        new Vector2(ui.PosOnCanvas.X + virtualRectInCanvas.GetWidth() * outputIndex + 1 + 3, ui.PosOnCanvas.Y - 1),
                        new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6, 3))
                    , DraftConnection.IsMatchingOutputType(outputDef.ValueType) ? Color.White : color);
            }
            ImGui.PopID();
        }


        public static ImRect GetOutputSlotSizeInCanvas(SymbolChildUi sourceUi, int outputIndex)
        {
            var outputCount = sourceUi.SymbolChild.Symbol.OutputDefinitions.Count;
            var inputWidth = sourceUi.Size.X / outputCount;   // size count must be non-zero in this method

            return ImRect.RectWithSize(
                new Vector2(sourceUi.PosOnCanvas.X + inputWidth * outputIndex + 1, sourceUi.PosOnCanvas.Y - 3),
                new Vector2(inputWidth - 2, 6));
        }


        public static void DrawInputSlot(SymbolChildUi targetUi, int inputIndex)
        {
            var inputDef = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex];
            var virtualRectInCanvas = GetInputSlotSizeInCanvas(targetUi, inputIndex);
            var rInScreen = GraphCanvas.Current.TransformRect(virtualRectInCanvas);

            ImGui.PushID(targetUi.SymbolChild.Id.GetHashCode() + inputIndex);
            ImGui.SetCursorScreenPos(rInScreen.Min);
            ImGui.InvisibleButton("input", rInScreen.GetSize());
            THelpers.DebugItemRect("input-slot");

            var valueType = inputDef.DefaultValue.ValueType;
            var color = ColorForType(inputDef);

            // Note: isItemHovered will not work
            var hovered = DraftConnection.TempConnection != null ? rInScreen.Contains(ImGui.GetMousePos())
                : ImGui.IsItemHovered();

            if (DraftConnection.IsInputSlotCurrentConnectionTarget(targetUi, inputIndex))
            {
                GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, ColorForType(inputDef));

                if (ImGui.IsMouseDragging(0))
                {
                    DraftConnection.Update();
                }
            }
            else if (hovered)
            {
                if (DraftConnection.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                    if (ImGui.IsMouseReleased(0))
                    {
                        DraftConnection.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputIndex);
                    }
                }
                else
                {
                    GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        DraftConnection.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputIndex);
                    }
                }
            }
            else
            {
                GraphCanvas.Current.DrawRectFilled(
                    ImRect.RectWithSize(
                        new Vector2(targetUi.PosOnCanvas.X + virtualRectInCanvas.GetWidth() * inputIndex + 1 + 3,
                                    targetUi.PosOnCanvas.Y + targetUi.Size.Y - T3Style.VisibleSlotHeight),
                        new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6,
                                    T3Style.VisibleSlotHeight))
                    , color: DraftConnection.IsMatchingInputType(inputDef.DefaultValue.ValueType) ? Color.White : color);
            }

            ImGui.PopID();
        }


        private static Color ColorForType(Symbol.InputDefinition inputDef)
        {
            return TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;
        }

        private static Color ColorForType(Symbol.OutputDefinition outputDef)
        {
            return TypeUiRegistry.Entries[outputDef.ValueType].Color;
        }

        public static ImRect GetInputSlotSizeInCanvas(SymbolChildUi targetUi, int inputIndex)
        {
            var inputCount = targetUi.SymbolChild.Symbol.InputDefinitions.Count;
            var inputWidth = inputCount == 0 ? targetUi.Size.X
                : targetUi.Size.X / inputCount;

            return ImRect.RectWithSize(
                new Vector2(targetUi.PosOnCanvas.X + inputWidth * inputIndex + 1, targetUi.PosOnCanvas.Y + targetUi.Size.Y - 3),
                new Vector2(inputWidth - 2, 6));
        }

    }
}
