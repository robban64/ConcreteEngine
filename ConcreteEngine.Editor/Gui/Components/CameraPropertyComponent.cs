#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Gui;

internal static class CameraPropertyComponent
{
    private struct CameraDataState
    {
        public CameraTransformDataState Transform;
        public CameraProjectionState Projection;
    }

    private const int WindowPaddingX = 12;

    private static CameraDataState _state;
    private static ref CameraTransformDataState TransformState => ref _state.Transform;
    private static ref CameraProjectionState ProjectionState => ref _state.Projection;

    private static CameraViewModel _cameraModel => StateCtx.CameraModel;

    public static void Init()
    {
        TransformState.From(in _cameraModel.Transform);
        ProjectionState = CameraProjectionState.FromModel(in _cameraModel.Projection);
    }

    public static void UpdateStateFromViewModel()
    {
        TransformState.FromStable(in _cameraModel.Transform);
        ProjectionState = CameraProjectionState.FromModel(in _cameraModel.Projection);
    }

    private static void OnUpdateTranslation()
    {
        ref var transform = ref _cameraModel.Transform;
        transform.Translation = TransformState.Translation;
        StateCtx.ExecuteSetCameraTransform(in _cameraModel.Model);
    }

    private static void OnUpdateScale()
    {
        ref var transform = ref _cameraModel.Transform;
        transform.Scale = TransformState.Scale;
        StateCtx.ExecuteSetCameraTransform(in _cameraModel.Model);
    }

    private static void OnUpdateRotation()
    {
        ref var transform = ref _cameraModel.Transform;
        transform.Orientation = YawPitch.FromVector2(TransformState.Orientation);
        StateCtx.ExecuteSetCameraTransform(in _cameraModel.Model);
    }

    private static void OnUpdateProjection()
    {
        _cameraModel.Projection = ProjectionState.ToModel();
        StateCtx.ExecuteSetCameraTransform(in _cameraModel.Model);
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
        var viewport = _cameraModel.Viewport;
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

        if (ImGui.InputFloat2("##camera-near-far", ref ProjectionState.NearFar, "%.2f"))
        {
            OnUpdateProjection();
        }

        ImGui.EndGroup();

        ImGui.Separator();

        ImGui.BeginGroup();

        ImGui.TextUnformatted("Field of view");
        if (ImGui.SliderFloat("##camera-fov", ref ProjectionState.Fov, StateLimits.MinFov, StateLimits.MaxFov,
                "%.2f"))
        {
            OnUpdateProjection();
        }

        ImGui.EndGroup();


        ImGui.EndGroup();
    }

    private static void DrawTransform()
    {
        ImGui.TextUnformatted("Translation");
        ImGui.Separator();
        if (ImGui.InputFloat3("##camera-translation", ref TransformState.Translation, "%.3f", ImGuiInputTextFlags.None))
        {
            OnUpdateTranslation();
        }

        ImGui.TextUnformatted("Scale");
        ImGui.Separator();
        if (ImGui.InputFloat3("##camera-scale", ref TransformState.Scale, "%.3f", ImGuiInputTextFlags.None))
        {
            OnUpdateScale();
        }

        ImGui.TextUnformatted("Rotation");
        ImGui.Separator();
        if (ImGui.InputFloat2("##camera-rotation", ref TransformState.Orientation, "%.3f", ImGuiInputTextFlags.None))
        {
            OnUpdateRotation();
        }
    }
}