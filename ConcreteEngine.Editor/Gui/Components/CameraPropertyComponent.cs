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

    private static ModelState<CameraViewModel> Model => EditorStateManager.CameraModelState;
    private static CameraViewModel ViewState => Model.State!;

    private static ref readonly CameraEditorPayload Data => ref ViewState.Data;
    private static ref CameraDataState DataState => ref ViewState.DataState;

    private static void OnUpdateData()
    {
        DataState.Fill(Data.Generation, Data.Viewport, out var payload);
        Model.TriggerEvent(EventKey.SelectionUpdated, in payload);
    }

    public static void Draw()
    {
        var size = new Vector2(GuiTheme.RightSidebarWidth - WindowPaddingX, 0);
        if (ImGui.BeginChild("##camera-properties", size, ImGuiChildFlags.AutoResizeY))
        {
            DrawInner();
            ImGui.EndChild();
        }
    }

    private static void DrawInner()
    {
        ImGui.SeparatorText("Viewport");
        DrawViewport();
        ImGui.Dummy(new Vector2(0, 4));
        ImGui.SeparatorText("Transform");
        ImGui.Dummy(new Vector2(0, 2));
        DrawTransform();
    }

    private static void DrawViewport()
    {
        var viewport = Data.Viewport;
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);
        //var width = ImGui.GetContentRegionAvail().X - WindowPaddingX;

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

        //
        ImGui.Separator();

        // Row 
        ImGui.BeginGroup();
        ImGui.TextUnformatted("Near: ");
        ImGui.SameLine();

        ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - WindowPaddingX * 2 - ImGui.CalcTextSize("Near: ").X);
        ImGui.TextUnformatted("Far: ");

        if (ImGui.InputFloat2("##camera-near-far", ref DataState.Projection.NearFar, "%.2f"))
        {
            OnUpdateData();
        }

        ImGui.EndGroup();

        ImGui.Separator();

        ImGui.BeginGroup();

        ImGui.TextUnformatted("Field of view");
        if (ImGui.SliderFloat("##camera-fov", ref DataState.Projection.Fov, StateLimits.MinFov, StateLimits.MaxFov,
                "%.2f"))
        {
            OnUpdateData();
        }

        ImGui.EndGroup();


        ImGui.EndGroup();
    }

    private static void DrawTransform()
    {
        ImGui.TextUnformatted("Translation");
        ImGui.Separator();
        if (ImGui.InputFloat3("##camera-translation", ref DataState.Transform.Translation, "%.3f",
                ImGuiInputTextFlags.None))
        {
            OnUpdateData();
        }

        ImGui.TextUnformatted("Scale");
        ImGui.Separator();
        if (ImGui.InputFloat3("##camera-scale", ref DataState.Transform.Scale, "%.3f", ImGuiInputTextFlags.None))
        {
            OnUpdateData();
        }

        ImGui.TextUnformatted("Rotation");
        ImGui.Separator();
        if (ImGui.InputFloat2("##camera-rotation", ref DataState.Transform.Orientation, "%.3f", ImGuiInputTextFlags.None))
        {
            OnUpdateData();
        }
    }
}