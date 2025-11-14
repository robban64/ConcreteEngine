#region

using System.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Editor.ViewModel;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Gui.Components;

internal static class CameraPropertyComponent
{
    private const int WindowPaddingX = 12;

    private static ModelState<CameraViewModel> Model => ModelManager.CameraState;
    private static CameraViewModel ViewModel => Model.State!;

    private static int _editedField = -1;

    private static void OnUpdateData()
    {
        var v = ViewModel;
        v.DataState.Fill(v.Data.Generation, v.Data.Viewport, out var payload);
        Model.TriggerEvent(EventKey.SelectionUpdated, in payload);
    }

    public static void Draw()
    {
        const ImGuiChildFlags flags = ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.AlwaysUseWindowPadding;
        var size = new Vector2(GuiTheme.RightSidebarWidth - WindowPaddingX, 0);

        if (!ImGui.BeginChild("##camera-properties", size, flags)) return;
        DrawInner();
        ImGui.EndChild();

        if (_editedField >= 0)
        {
            OnUpdateData();
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
        var viewport = ViewModel.Data.Viewport;
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
        ref var proj = ref ViewModel.DataState.Projection;
        var fieldStatus = new ImGuiFieldStatus();

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Near / Far");

        ImGui.InputFloat2("##camera-near-far", ref proj.NearFar, "%.2f");
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
        ref var state = ref ViewModel.DataState.Transform;
        var fieldStatus = new ImGuiFieldStatus();

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Translation");
        ImGui.Separator();
        
        ImGui.InputFloat3("##camera-translation", ref state.Translation, "%.3f");
        fieldStatus.NextField();

        ImGui.TextUnformatted("Scale");
        ImGui.Separator();
        ImGui.InputFloat3("##camera-scale", ref state.Scale, "%.3f");
        fieldStatus.NextField();

        ImGui.TextUnformatted("Rotation");
        ImGui.Separator();
        ImGui.InputFloat2("##camera-rotation", ref state.Orientation, "%.3f");
        fieldStatus.NextField();
        ImGui.EndGroup();

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }
}