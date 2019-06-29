﻿using ImGuiNET;
using T3.Gui.Graph;
using T3.Gui.TypeColors;

namespace T3.Gui
{
    public class UiSettingsWindow
    {
        //private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);
        public static bool UseVSync => _vsync;
        private static bool _vsync = true;

        public static bool WindowRegionsVisible;
        public static bool ItemRegionsVisible;
        public static bool DemoWindowVisible;
        public static bool ConsoleWindowVisible = true;
        public static bool ParameterWindowVisible = true;
        public static bool CurveEditorVisible = true;

        public static unsafe void DrawUiSettings()
        {
            ImGui.Begin("Stats");
            {
                Metrics.Draw();
                ImGui.Checkbox("VSync", ref _vsync);
                ImGui.Checkbox("Show Window Regions", ref WindowRegionsVisible);
                ImGui.Checkbox("Show Item Regions", ref ItemRegionsVisible);
                ImGui.Checkbox("Demo Window Visible", ref DemoWindowVisible);
                ImGui.Checkbox("Console Window Visible", ref ConsoleWindowVisible);
                ImGui.Checkbox("Curve Editor Visible", ref CurveEditorVisible);
                ImGui.Checkbox("Parameters visible", ref ParameterWindowVisible);

                if (ImGui.Button("Open new Graph Canvas"))
                {
                    T3UI.OpenNewGraphWindow();
                }
                ColorVariations.DrawSettingsUi();
                if (ImGui.TreeNode("Styles"))
                {
                    ImGui.DragFloat("Height Connection Zone", ref GraphOperator._connectionZoneHeight);
                    ImGui.DragFloat2("Label position", ref GraphOperator._labelPos);
                    ImGui.TreePop();
                }
            }
            ImGui.End();
        }
    }
}
