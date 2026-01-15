using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components;

internal sealed class CameraComponent : EditorComponent<SlotState<EditorCameraState>>
{
    public override void DrawRight(SlotState<EditorCameraState> state, in FrameContext ctx)
    {
        if (!ImGui.BeginChild("##camera-properties"u8, ImGuiChildFlags.AlwaysUseWindowPadding)) return;

        ref var data = ref state.State;

        ImGui.SeparatorText("Viewport"u8);
        DrawViewport(data.Viewport, in ctx);
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


    private static void DrawViewport(Size2D viewport, in FrameContext ctx)
    {
        var sw = ctx.Writer;

        ImGui.BeginGroup();

        // Row 
        ImGui.BeginGroup();
        ImGui.TextUnformatted("Width:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(sw.Write(viewport.Width));
        ImGui.EndGroup();

        ImGui.SameLine();
        ImGui.TextUnformatted("-"u8);
        ImGui.SameLine();

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Height:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(sw.Write(viewport.Height));
        ImGui.EndGroup();

        // Row 

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Aspect Ratio:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(sw.Write(viewport.AspectRatio, "F2"));
        ImGui.EndGroup();
        
        //
        ImGui.EndGroup();
    }

    private static bool DrawProjection(ref ProjectionInfo projection)
    {
        var fieldStatus = new FormFieldStatus();

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Near / Far"u8);

        var nearFar = new Vector2(projection.Near, projection.Far);
        if (ImGui.InputFloat2("##camera-near-far", ref nearFar, "%.2f"))
        {
            projection.Near = nearFar.X;
            projection.Far = nearFar.Y;
        }

        fieldStatus.NextField();

        ImGui.EndGroup();
        ImGui.Separator();
        ImGui.BeginGroup();

        ImGui.TextUnformatted("Field of view"u8);
        ImGui.SliderFloat("##camera-fov", ref projection.Fov, StateLimits.MinFov, StateLimits.MaxFov, "%.2f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.EndGroup();

        return fieldStatus.HasEdited(out _);
    }

    private static bool DrawTransform(ref ViewTransform t)
    {
        var fieldStatus = new FormFieldStatus();

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Transform"u8);
        ImGui.Separator();

        ImGui.InputFloat3("##camera-translation", ref t.Translation, "%.3f");
        fieldStatus.NextField();

        ImGui.TextUnformatted("Rotation"u8);
        ImGui.Separator();
        ImGui.InputFloat2("##camera-rotation", ref Unsafe.As<YawPitch, Vector2>(ref t.Orientation), "%.3f");
        fieldStatus.NextField();
        ImGui.EndGroup();

        return fieldStatus.HasEdited(out _);
    }
}