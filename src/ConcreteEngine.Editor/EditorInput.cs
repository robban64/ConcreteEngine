using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor;

internal static class EditorInput
{
    public static EditorInputState InputState;
    public static bool IsInteracting() => InputState.IsInteracting;
    public static bool IsBlockingEngine() => InputState.HasActiveInput || InputState.HasActiveMouse;

    public static void Prepare()
    {
        var io = ImGui.GetIO();
        ref var state = ref InputState;
        state.HasActiveInput = io.WantTextInput || ImGui.IsAnyItemActive() || ImGui.IsAnyItemFocused();
        state.HasActiveMouse = io.WantCaptureMouse;

        state.IsDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
        state.IsInteracting = state.IsDragging || ImGui.IsItemClicked(ImGuiMouseButton.Left);
        
        state.IsLeftClick = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        state.IsRightClick = ImGui.IsMouseClicked(ImGuiMouseButton.Right);
    }



    private static void CheckHotkeys()
    {
        if (ImGui.IsItemFocused()) return;
/*
        if (ImGui.IsKeyDown(ImGuiKey.Key1)) states.SetLeftSidebarState(LeftSidebarMode.Assets);
        else if (ImGui.IsKeyDown(ImGuiKey.Key2)) states.SetLeftSidebarState(LeftSidebarMode.Scene);
        else if (ImGui.IsKeyDown(ImGuiKey.Key3)) states.SetRightSidebarState(RightSidebarMode.Camera);
        else if (ImGui.IsKeyDown(ImGuiKey.Key4)) states.SetRightSidebarState(RightSidebarMode.Visuals);
        else if (ImGui.IsKeyDown(ImGuiKey.Key5)) states.SetRightSidebarState(RightSidebarMode.Sky);
        else if (ImGui.IsKeyDown(ImGuiKey.Key6)) states.SetRightSidebarState(RightSidebarMode.Terrain);
        */
    }
}