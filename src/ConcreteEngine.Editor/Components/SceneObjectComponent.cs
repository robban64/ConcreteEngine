using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal static class SceneObjectComponent
{
    private sealed class SceneObjectSelection
    {
        public SceneObjectId LastId;
        public string IdString = string.Empty;
        public string GuidString = string.Empty;
        public string Name = string.Empty;
    }
    
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;

    private static SceneObjectSelection? _selection;

    public static void Draw()
    {
        const ImGuiChildFlags childFlags = ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY;
        const ImGuiWindowFlags windowFlags =
            ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.NoBringToFrontOnFocus;

        var selected = EditorDataStore.SelectedSceneObj;
        if (!selected.IsValid()) return;

        if (_selection is null || _selection.LastId != selected)
        {
            var sceneObject = EngineController.SceneController.GetSceneObject(selected);
            _selection = new SceneObjectSelection
            {
                LastId = sceneObject.Id,
                IdString = sceneObject.Id.ToString(),
                GuidString = sceneObject.GId.ToString(),
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
    
    
    public static void DrawProperties()
    {
        if (!EditorDataStore.SelectedSceneObj.IsValid()) return;

        float childHeight = ImGui.GetContentRegionAvail().Y - 2;
        if (ImGui.BeginChild("##right-sidebar-properties"u8, new Vector2(0, childHeight),
                ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY,
                ImGuiWindowFlags.AlwaysVerticalScrollbar |
                ImGuiWindowFlags.NoBringToFrontOnFocus))
        {
            DrawCoreProperties();
            /*
            var componentRef = EditorDataStore.EntityState.ComponentRef;
            if (!componentRef.IsValid)
            {
                ImGui.EndChild();
                return;
            }

            ImGui.Dummy(new Vector2(0, 4));

            if (componentRef.ItemType == EditorItemType.Animation)
                DrawAnimationProperties();
            else if (componentRef.ItemType == EditorItemType.Particle)
                DrawParticleProperties();
                */
            ImGui.EndChild();
        }
    }

    private static void DrawCoreProperties()
    {
        ref var transform = ref EditorDataStore.Slot<TransformStable>.State;
        var fieldStatus = new ImGuiFieldStatus();

        ImGui.SeparatorText("Entity Component"u8);
        ImGui.TextUnformatted("Model:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted("0"u8);

        ImGui.TextUnformatted("Material:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted("0"u8);

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform"u8);

        ImGui.TextUnformatted("Translation"u8);
        ImGui.Separator();
        ImGui.InputFloat3("##ent-prop-translation", ref transform.Translation, "%.3f", ImGuiInputTextFlags.None);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Scale"u8);
        ImGui.Separator();
        ImGui.InputFloat3("##ent-prop-scale", ref transform.Scale, "%.3f", ImGuiInputTextFlags.None);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Rotation"u8);
        ImGui.Separator();
        ImGui.InputFloat3("##ent-prop-rotation", ref transform.EulerAngles, "%.3f", ImGuiInputTextFlags.None);
        var rotationField = fieldStatus.NextField();

        if (fieldStatus.HasEdited(out _))
        {
            if (rotationField != -1)
                transform.ApplyRotationFromEuler();

            EngineController.CommitEntity();
        }
    }
    


}