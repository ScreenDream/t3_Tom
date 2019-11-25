using System;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using T3.Gui.Windows.TimeLine;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    public class GraphWindow : Window
    {
        public GraphCanvas GraphCanvas { get; private set; }

        public GraphWindow()
        {
            _instanceCounter++;
            Config.Title = "Graph##" + _instanceCounter;
            Config.Visible = true;
            AllowMultipleInstances = true;

            const string trackName = @"Resources\lorn-sega-sunset.mp3";
            _clipTime = File.Exists(trackName) ? new StreamClipTime(trackName) : new ClipTime();

            var opId = UserSettings.GetLastOpenOpForWindow(Config.Title);

            var shownOp = (opId != Guid.Empty
                               ? FindIdInNestedChildren(T3Ui.UiModel.MainOp, opId)
                               : null) ?? T3Ui.UiModel.MainOp;

            GraphCanvas = new GraphCanvas(this, shownOp);

            _timeLineCanvas = new TimeLineCanvas(_clipTime);

            WindowFlags = ImGuiWindowFlags.NoScrollbar;
            GraphWindowInstances.Add(this);
        }

        private Instance FindIdInNestedChildren(Instance instance, Guid childId)
        {
            foreach (var child in instance.Children)
            {
                if (child.Id == childId)
                {
                    return child;
                }

                var result = FindIdInNestedChildren(child, childId);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static int _instanceCounter;
        private static readonly List<Window> GraphWindowInstances = new List<Window>();

        public override List<Window> GetInstances()
        {
            return GraphWindowInstances;
        }

        protected override void UpdateBeforeDraw()
        {
            _clipTime.Update();
        }

        protected override void DrawAllInstances()
        {
            foreach (var w in GraphWindowInstances)
            {
                w.DrawOneInstance();
            }
        }

        private static bool _justAddedDescription;

        protected override void DrawContent()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            {
                var dl = ImGui.GetWindowDrawList();

                CustomComponents.SplitFromBottom(ref _heightTimeLine);
                var graphHeight = ImGui.GetWindowHeight() - _heightTimeLine - 30;

                ImGui.BeginChild("##graph", new Vector2(0, graphHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                {
                    dl.ChannelsSplit(2);
                    dl.ChannelsSetCurrent(1);
                    {
                        DrawBreadcrumbs();

                        ImGui.SetCursorPosX(8);
                        ImGui.PushFont(Fonts.FontBold);
                        ImGui.Text(GraphCanvas.CompositionOp.Symbol.Name);
                        ImGui.PopFont();
                        ImGui.SameLine();

                        ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.5f).Rgba);
                        ImGui.Text(" in " + GraphCanvas.CompositionOp.Symbol.Namespace);
                        ImGui.PopStyleColor();

                        var symbolUi = SymbolUiRegistry.Entries[GraphCanvas.CompositionOp.Symbol.Id];

                        if (symbolUi.Description == null)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                            ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);

                            ImGui.PushFont(Fonts.FontSmall);
                            if (ImGui.Button("add description..."))
                            {
                                symbolUi.Description = " ";
                                _justAddedDescription = false;
                            }

                            ImGui.PopFont();
                            ImGui.PopStyleColor(2);
                        }
                        else
                        {
                            if (symbolUi.Description == string.Empty)
                            {
                                symbolUi.Description = null;
                            }
                            else
                            {
                                var desc = symbolUi.Description;
                                ImGui.PushFont(Fonts.FontSmall);
                                ImGui.PushStyleColor(ImGuiCol.FrameBg, Color.Transparent.Rgba);
                                ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                                {
                                    var sizeMatchingDescription = ImGui.CalcTextSize(desc) + new Vector2(20, 40);
                                    sizeMatchingDescription.X = Im.Max(300, sizeMatchingDescription.X);
                                    if (_justAddedDescription)
                                    {
                                        ImGui.SetKeyboardFocusHere();
                                        _justAddedDescription = false;
                                    }

                                    ImGui.InputTextMultiline("##description", ref desc, 3000, sizeMatchingDescription);
                                }
                                ImGui.PopStyleColor(2);
                                ImGui.PopFont();
                                symbolUi.Description = desc;
                            }
                        }

                        TimeControls.DrawTimeControls(_clipTime, ref _timeLineCanvas.Mode);
                    }
                    dl.ChannelsSetCurrent(0);
                    GraphCanvas.Draw(dl);
                    dl.ChannelsMerge();
                }
                ImGui.EndChild();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4);
                ImGui.BeginChild("##timeline", Vector2.Zero, false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                {
                    DrawTimelineAndCurveEditor();
                }
                ImGui.EndChild();
            }
            ImGui.PopStyleVar();
        }

        protected override void Close()
        {
            GraphWindowInstances.Remove(this);
        }

        protected override void AddAnotherInstance()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new GraphWindow();    // Must call constructor
        }

        private void DrawTimelineAndCurveEditor()
        {
            _timeLineCanvas.Draw(GraphCanvas.CompositionOp, GetCurvesForSelectedNodes());
        }

        public struct AnimationParameter
        {
            public IEnumerable<Curve> Curves;
            public IInputSlot Input;
            public Instance Instance;
        }

        private List<AnimationParameter> GetCurvesForSelectedNodes()
        {
            var selection = GraphCanvas.SelectionHandler.SelectedElements;
            var symbolUi = SymbolUiRegistry.Entries[GraphCanvas.CompositionOp.Symbol.Id];
            var animator = symbolUi.Symbol.Animator;
            var curvesForSelection = (from child in GraphCanvas.CompositionOp.Children
                                      from selectedElement in selection
                                      where child.Id == selectedElement.Id
                                      from input in child.Inputs
                                      where animator.IsInputSlotAnimated(input)
                                      select new AnimationParameter()
                                             {
                                                 Instance = child,
                                                 Input = input,
                                                 Curves = animator.GetCurvesForInput(input)
                                             }).ToList();
            return curvesForSelection;
        }

        private void DrawBreadcrumbs()
        {
            ImGui.SetCursorScreenPos(ImGui.GetWindowPos() + new Vector2(1, 1));
            IEnumerable<Instance> parents = GraphCanvas.GetParents();

            ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
            ImGui.PushFont(Fonts.FontSmall);
            {
                foreach (var p in parents)
                {
                    ImGui.SameLine();
                    ImGui.PushID(p.Id.GetHashCode());

                    var clicked = ImGui.Button(p.Symbol.Name);

                    if (clicked)
                    {
                        GraphCanvas.OpenComposition(p, zoomIn: false);
                        break;
                    }

                    ImGui.SameLine();
                    ImGui.PopID();
                    ImGui.Text(">");
                }
            }
            ImGui.PopFont();
            ImGui.PopStyleColor();
        }

        private readonly ClipTime _clipTime;
        private static float _heightTimeLine = 200;
        private readonly TimeLineCanvas _timeLineCanvas;
    }
}