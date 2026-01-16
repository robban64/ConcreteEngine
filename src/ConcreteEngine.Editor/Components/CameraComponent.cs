using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal sealed class CameraComponent : EditorComponent<SlotState<EditorCameraState>>
{
    public override void DrawRight(SlotState<EditorCameraState> state, ref FrameContext ctx)
    {
        if (!ImGui.BeginChild("##camera-properties"u8, ImGuiChildFlags.AlwaysUseWindowPadding)) return;

        ref var data = ref state.Data;

        ImGui.SeparatorText("Viewport"u8);
        DrawViewport(data.Viewport, ref ctx);
        ImGui.Dummy(new Vector2(0, 2));

        ImGui.SeparatorText("Transform"u8);
        var hasChangeTransform = DrawTransform(ref data.Transform);
        ImGui.Dummy(new Vector2(0, 2));

        ImGui.SeparatorText("Projection"u8);
        var hasChangeProjection = DrawProjection(ref data.Projection);

        ImGui.EndChild();

        if (hasChangeTransform || hasChangeProjection)
        {
            TriggerEvent(EventKey.CommitVisualData, EmptyEvent.Empty);
        }
    }


    private static void DrawViewport(Size2D viewport, ref FrameContext ctx)
    {
        ref var sw = ref  ctx.Sw;
        ImGui.BeginGroup();
        new TextLayout().Property("Width:"u8, sw.Write(viewport.Width))
            .SameLineProperty()
            .Property("Height:"u8, sw.Write(viewport.Height))
            .Property("Aspect Ratio:"u8, sw.Write(viewport.AspectRatio, "F2"));
        ImGui.EndGroup();
    }

    private static bool DrawProjection(ref ProjectionInfo projection)
    {
        const float min = StateLimits.MinFov;
        const float max = StateLimits.MaxFov;

        var fieldStatus = new FormFieldStatus();
        ref var vProj = ref Unsafe.As<ProjectionInfo, Vector2>(ref projection);

        ImGui.BeginGroup();
        fieldStatus.InputFloat2("Near / Far"u8, "##camera-near-far", ref vProj, "%.2f");
        fieldStatus.SliderFloat("Field of view"u8, "##camera-fov", ref projection.Fov, min, max, "%.2f");
        ImGui.EndGroup();

        return fieldStatus.HasEdited(out _);
    }

    private static bool DrawTransform(ref ViewTransform t)
    {
        var fieldStatus = new FormFieldStatus();

        ImGui.BeginGroup();
        ref var orientation = ref Unsafe.As<YawPitch, Vector2>(ref t.Orientation);
        fieldStatus.InputFloat3("Transform"u8, "##camera-translation", ref t.Translation, "%.3f");
        fieldStatus.InputFloat2("Rotation"u8, "##camera-rotation", ref orientation, "%.3f");
        ImGui.EndGroup();


        return fieldStatus.HasEdited(out _);
    }
}