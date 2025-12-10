#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Components;

internal static class CameraComponent
{
    private const int WindowPaddingX = 12;

    private static ModelStateContext<CameraState> Context => ModelManager.CameraStateContext;
    private static CameraState State => Context.State!;

    private static int _editedField = -1;


    public static void Draw()
    {
        const ImGuiChildFlags flags = ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.AlwaysUseWindowPadding;
        var size = new Vector2(GuiTheme.RightSidebarWidth - WindowPaddingX, 0);

        if (!ImGui.BeginChild("##camera-properties", size, flags)) return;
        DrawInner();
        ImGui.EndChild();

        if (_editedField >= 0)
        {
            State.TriggerWrite();
            _editedField = -1;
        }
    }

    private static void DrawInner()
    {
        ImGui.SeparatorText("Viewport");
        DrawViewport();
        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform");
        DrawTransform();
        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Projection");
        DrawProjection();
    }

    private static void DrawViewport()
    {
        var viewport = State.DataState.Viewport;
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

    private static void DrawProjection()
    {
        ref var proj = ref State.DataState.Projection;
        var fieldStatus = new ImGuiFieldStatus();

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Near / Far");

        Vector2 nearFar = new Vector2(proj.Near, proj.Far);
        if (ImGui.InputFloat2("##camera-near-far", ref nearFar, "%.2f"))
        {
            proj.Near = nearFar.X;
            proj.Far = nearFar.Y;
        }
        fieldStatus.NextField();

        ImGui.EndGroup();
        ImGui.Separator();
        ImGui.BeginGroup();

        ImGui.TextUnformatted("Field of view");
        ImGui.SliderFloat("##camera-fov", ref proj.Fov, StateLimits.MinFov, StateLimits.MaxFov, "%.2f");
        fieldStatus.NextFieldDrag();


        ImGui.PopItemWidth();
        ImGui.EndGroup();

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }

    private static void DrawTransform()
    {
        ref var state = ref State.DataState.Transform;
        var fieldStatus = new ImGuiFieldStatus();

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Transform");
        ImGui.Separator();

        ImGui.InputFloat3("##camera-translation", ref state.Translation, "%.3f");
        fieldStatus.NextField();

        ImGui.TextUnformatted("Rotation");
        ImGui.Separator();
        var orientation = state.Orientation.AsVec2();
        if (ImGui.InputFloat2("##camera-rotation", ref orientation, "%.3f"))
        {
            state.Orientation = YawPitch.FromVector2(orientation);
        }
        fieldStatus.NextField();
        ImGui.EndGroup();

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }
}