using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

namespace ConcreteEngine.Editor.Components;

internal static class CameraComponent
{
    private const int WindowPaddingX = 12;

    public static void Draw()
    {
        const ImGuiChildFlags flags = ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.AlwaysUseWindowPadding;
        var size = new Vector2(GuiTheme.RightSidebarWidth - WindowPaddingX, 0);

        if (!ImGui.BeginChild("##camera-properties", size, flags)) return;
        var hasChange = DrawInner();
        ImGui.EndChild();

        if (hasChange) EngineController.CommitCamera();
    }

    private static bool DrawInner()
    {
        ref var state = ref EditorDataStore.Slot<EditorCameraState>.State;

        ImGui.SeparatorText("Viewport");
        DrawViewport(ref state);
        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform");
        var hasChangeTransform = DrawTransform(ref state);
        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Projection");
        var hasChangeProjection = DrawProjection(ref state);

        return hasChangeTransform || hasChangeProjection;
    }

    private static void DrawViewport(ref EditorCameraState state)
    {
        var viewport = state.Viewport;
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);

        ImGui.BeginGroup();

        // Row 
        ImGui.BeginGroup();
        ImGui.TextUnformatted("Width:");
        ImGui.SameLine();
        ImGui.TextUnformatted(formatter.Format(viewport.Width));
        ImGui.EndGroup();

        ImGui.SameLine();
        ImGui.TextUnformatted("-");
        ImGui.SameLine();

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Height:");
        ImGui.SameLine();
        ImGui.TextUnformatted(formatter.Format(viewport.Height));
        ImGui.EndGroup();

        // Row 
        ImGui.BeginGroup();
        ImGui.TextUnformatted("Aspect Ratio:");
        ImGui.SameLine();
        ImGui.TextUnformatted(formatter.Format(viewport.AspectRatio));
        ImGui.EndGroup();
        ImGui.EndGroup();
    }

    private static bool DrawProjection(ref EditorCameraState state)
    {
        var fieldStatus = new ImGuiFieldStatus();
        ref var projection = ref state.Projection;

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Near / Far");

        Vector2 nearFar = new Vector2(projection.Near, projection.Far);
        if (ImGui.InputFloat2("##camera-near-far", ref nearFar, "%.2f"))
        {
            projection.Near = nearFar.X;
            projection.Far = nearFar.Y;
        }

        fieldStatus.NextField();

        ImGui.EndGroup();
        ImGui.Separator();
        ImGui.BeginGroup();

        ImGui.TextUnformatted("Field of view");
        ImGui.SliderFloat("##camera-fov", ref state.Projection.Fov, StateLimits.MinFov, StateLimits.MaxFov, "%.2f");
        fieldStatus.NextFieldDrag();


        ImGui.PopItemWidth();
        ImGui.EndGroup();

        return fieldStatus.HasEdited(out _);
    }

    private static bool DrawTransform(ref EditorCameraState state)
    {
        var fieldStatus = new ImGuiFieldStatus();
        ref var t = ref state.Transform;

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Transform");
        ImGui.Separator();

        ImGui.InputFloat3("##camera-translation", ref t.Translation, "%.3f");
        fieldStatus.NextField();

        ImGui.TextUnformatted("Rotation");
        ImGui.Separator();
        var orientation = t.Orientation.AsVec2();
        if (ImGui.InputFloat2("##camera-rotation", ref orientation, "%.3f"))
        {
            t.Orientation = YawPitch.FromVector2(orientation);
        }

        fieldStatus.NextField();
        ImGui.EndGroup();

        return fieldStatus.HasEdited(out _);
    }
}