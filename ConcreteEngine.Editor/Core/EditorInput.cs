#region

using System.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Core;

internal static class EditorInput
{
    private static Vector2 _prevMousePos;

    private static bool _isDragging;
    private static bool _wasDragging;


    public static bool BlockInput()
    {
        var io = ImGui.GetIO();

        var blockKeyboard = io.WantTextInput || io.WantCaptureKeyboard || ImGui.IsAnyItemActive() ||
                            ImGui.IsAnyItemFocused();

        //var anyMouseDown = io.MouseDown[0] || io.MouseDown[1] || io.MouseDown[2] || io.MouseDown[3] || io.MouseDown[4];
        var overUi = ImGui.IsAnyItemHovered() || ImGui.IsAnyItemActive() ||
                     ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);

        var blockMouse = ImGui.IsAnyMouseDown() && overUi;

        if (ImGui.IsPopupOpen(null, ImGuiPopupFlags.AnyPopupId))
            blockMouse |= ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);

        return blockKeyboard || blockMouse;
    }


    public static bool IsMouseOverEditor()
    {
        var io = ImGui.GetIO();
        if (io.WantCaptureMouse || ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows))
            return true;

        return false;
    }

    public static void UpdateKeybinding()
    {
        CheckHotkeys();
    }

    private static void CheckHotkeys()
    {
        if (ImGui.IsItemFocused()) return;

        if (ImGui.IsKeyDown(ImGuiKey._1)) StateContext.SetLeftSidebarState(LeftSidebarMode.Assets);
        else if (ImGui.IsKeyDown(ImGuiKey._2)) StateContext.SetLeftSidebarState(LeftSidebarMode.Entities);
        else if (ImGui.IsKeyDown(ImGuiKey._3)) StateContext.SetRightSidebarState(RightSidebarMode.Camera);
        else if (ImGui.IsKeyDown(ImGuiKey._4)) StateContext.SetRightSidebarState(RightSidebarMode.World);
        else if (ImGui.IsKeyDown(ImGuiKey._5)) StateContext.SetRightSidebarState(RightSidebarMode.Sky);
        else if (ImGui.IsKeyDown(ImGuiKey._6)) StateContext.SetRightSidebarState(RightSidebarMode.Terrain);
    }


    public static void UpdateMouse(float delta)
    {
        var mousePos = ImGui.GetMousePos();
        var deltaAbs = Vector2.Abs(mousePos - _prevMousePos);

        _isDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
        var isClick = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var isReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Left);

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            ref var selection = ref EditorDataStore.Input.EditorSelection;
            selection.Id = EditorId.Empty;
            selection.Action = EditorMouseAction.None;
            selection.IsRequesting = false;
            selection.IsDirty = true;
            selection.RefreshTime();
            return;
        }

        EditorDataStore.Input.MouseState.SetPosition(mousePos, deltaAbs);
        

        if (isReleased && !_wasDragging)
        {
            HandleClick();
        }
        else if (_isDragging)
        {
            if (!_wasDragging)
                HandleClick();

            HandleDrag(deltaAbs);
        }

        _wasDragging = _isDragging;
        _prevMousePos = mousePos;
    }

    private static void HandleClick()
    {
        ref var state = ref EditorDataStore.Input.EditorSelection;
        state.Action = EditorMouseAction.RaycastSelect;
        state.RefreshTime();
    }

    private static void HandleDrag(Vector2 deltaAbs)
    {
        ref var state = ref EditorDataStore.Input.EditorSelection;
        var action = state.Id.IsValid ? EditorMouseAction.RaycastDragTerrain : EditorMouseAction.None;
        var hasDelta = deltaAbs.X > 0 || deltaAbs.Y > 0;
        if (!hasDelta)
        {
            state.RefreshTime();
            return;
        }

        state.Action = action;
        state.RefreshTime();
    }
}