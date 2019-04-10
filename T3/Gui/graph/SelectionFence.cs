﻿

using ImGuiNET;
using imHelpers;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Input;
using t3.graph;
using T3.Gui.Selection;

namespace T3.Gui.Graph
{
    public class SelectionFence
    {
        public SelectionFence(SelectionHandler sh, GraphCanvasWindow canvas)
        {
            _selectionHandler = sh;
            _canvas = canvas;
        }

        public void Draw()
        {
            if (!isVisible)
            {
                if (ImGui.IsMouseClicked(0))
                {
                    HandleDragStarted();
                }
            }
            else
            {
                if (!ImGui.IsMouseReleased(0))
                {
                    HandleDragDelta();
                }
                else
                {
                    HandleDragCompleted();
                }
                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRectFilled(_startPositionInScreen, _dragPositionInScreen, TColors.ToUint(1, 1, 1, 0.5f), 1);
            }
        }

        public void HandleDragStarted()
        {
            var mouseMouse = ImGui.GetMousePos();
            _startPositionInScreen = mouseMouse;
            _dragPositionInScreen = mouseMouse;

            isVisible = true;
        }


        public void HandleDragDelta()
        {
            var _selectMode = SelectMode.Replace;
            _dragPositionInScreen = ImGui.GetMousePos();
            var delta = _startPositionInScreen - _dragPositionInScreen;
            var bounds = ImRect.RectBetweenPoints(_startPositionInScreen, _dragPositionInScreen);
            var boundsInCanvas = bounds; //ToDo: Implement!


            if (ImGui.IsKeyPressed((int)Key.LeftShift))
            {
                _selectMode = SelectMode.Add;
            }
            else if (ImGui.IsKeyPressed((int)Key.LeftCtrl))
            {
                _selectMode = SelectMode.Remove;
            }


            if (!_selectionStarted)
            {
                if (_dragPositionInScreen == _startPositionInScreen)
                    return;

                _selectionStarted = true;
                if (_selectMode == SelectMode.Replace)
                {
                    if (_selectionHandler != null)
                        _selectionHandler.Clear();
                }
            }

            if (_selectionHandler != null)
            {
                List<ISelectable> elementsToSelect = new List<ISelectable>();
                foreach (var child in _canvas.UiChildrenById.Values)
                {
                    var selectableWidget = child as ISelectable;
                    if (selectableWidget != null)
                    {
                        var rect = new ImRect(child.Position, child.Position + child.Size);
                        if (rect.Overlaps(boundsInCanvas))
                        {
                            elementsToSelect.Add(selectableWidget);
                        }
                    }
                }

                switch (_selectMode)
                {
                    case SelectMode.Add:
                        _selectionHandler.AddElements(elementsToSelect);
                        break;

                    case SelectMode.Remove:
                        _selectionHandler.RemoveElements(elementsToSelect);
                        break;

                    case SelectMode.Replace:
                        _selectionHandler.SetElements(elementsToSelect);
                        break;
                }
            }
        }

        public void HandleDragCompleted()
        {
            _selectionStarted = false;
            var newPosition = ImGui.GetMousePos();
            var delta = _startPositionInScreen - newPosition;
            var hasOnlyClicked = delta.LengthSquared() > 4f;
            if (hasOnlyClicked)
            {
                _selectionHandler.Clear();
            }
            isVisible = false;
        }


        private enum SelectMode
        {
            Add = 0,
            Remove,
            Replace,
        }


        private bool isVisible = false;
        private SelectionHandler _selectionHandler;
        private Vector2 _startPositionInScreen;
        private Vector2 _dragPositionInScreen;
        private GraphCanvasWindow _canvas;
        private bool _selectionStarted = false; // Set to true after DragThreshold reached
    }
}
