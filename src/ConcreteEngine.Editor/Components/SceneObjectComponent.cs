using System.Numerics;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using Hexa.NET.ImGui;
using ZaString.Core;

namespace ConcreteEngine.Editor.Components;

internal static class SceneObjectComponent
{
    private sealed class SceneObjectSelection
    {
        public EditorId LastId;
        public string IdString = string.Empty;
        public string GuidString = string.Empty;
        public string Name = string.Empty;
    }

    private static SceneObjectSelection? _selection;

    public static void Draw()
    {
        const ImGuiChildFlags childFlags = ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY;
        const ImGuiWindowFlags windowFlags =
            ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.NoBringToFrontOnFocus;

        var selected = EditorDataStore.SelectedSceneObject;
        if (!selected.IsValid) return;

        if (_selection is null || _selection.LastId != selected)
        {
            if (!ManagedStore.TryGet<EditorSceneObject>(selected, out var sceneObject))
            {
                ImGui.TextUnformatted("Invalid indices"u8);
                return;
            }

            _selection = new SceneObjectSelection
            {
                LastId = sceneObject.Id,
                IdString = sceneObject.Id.Identifier.ToString(),
                GuidString = sceneObject.EngineGid.ToString(),
            };
        }

        var size = new Vector2(0, ImGui.GetContentRegionAvail().Y - 2);
        if (ImGui.BeginChild("##right-sidebar-scene-obj"u8, size, childFlags, windowFlags))
        {
            ImGui.SeparatorText("Scene Object"u8);
            ImGui.Dummy(new Vector2(0, 4));

            DrawInfo();

            ImGui.EndChild();
        }

    }

    private static void DrawInfo()
    {
        ImGui.TextUnformatted("Id:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(_selection!.IdString);

        ImGui.TextUnformatted("GID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(_selection!.GuidString);

        ImGui.TextUnformatted("Name:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(_selection!.Name);
    }
}