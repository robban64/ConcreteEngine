using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Components.Layout;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components;

internal static class CameraComponent
{

    public static void Draw()
    {
        const ImGuiChildFlags flags =  ImGuiChildFlags.AlwaysUseWindowPadding;
        var size = new Vector2(RightSidebar.Width - GuiTheme.WindowPadding.X, 0);

        var hasChange = false;
        if (ImGui.BeginChild("##camera-properties"u8, size, flags))
        {
            hasChange = DrawInner();
            ImGui.EndChild();
        }

        if (hasChange) EngineController.CommitCamera();
    }

    private static bool DrawInner()
    {
        ref var state = ref StoreHub.Slot<EditorCameraState>.State;
        Span<byte> buffer = stackalloc byte[32];

        ImGui.SeparatorText("Viewport"u8);
        DrawViewport(ref state, buffer);
        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform"u8);
        var hasChangeTransform = DrawTransform(ref state);
        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Projection"u8);
        var hasChangeProjection = DrawProjection(ref state);

        return hasChangeTransform || hasChangeProjection;
    }

    private static void DrawViewport(ref EditorCameraState state, Span<byte> buffer)
    {
        var viewport = state.Viewport;
        var za = ZaUtf8SpanWriter.Create(buffer);
        za.Clear();

        ImGui.BeginGroup();

        // Row 
        ImGui.BeginGroup();
        ImGui.TextUnformatted("Width:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.Append(viewport.Width).AppendEndOfBuffer().AsSpan());
        ImGui.EndGroup();
        za.Clear();

        ImGui.SameLine();
        ImGui.TextUnformatted("-"u8);
        ImGui.SameLine();

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Height:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.Append(viewport.Height).AppendEndOfBuffer().AsSpan());
        ImGui.EndGroup();
        za.Clear();

        // Row 
        
        ImGui.BeginGroup();
        ImGui.TextUnformatted("Aspect Ratio:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.Append(viewport.AspectRatio, "F2").AppendEndOfBuffer().AsSpan());
        ImGui.EndGroup();
        ImGui.EndGroup();
        za.Clear();
    }

    private static bool DrawProjection(ref EditorCameraState state)
    {
        var fieldStatus = new ImGuiFieldStatus();
        ref var projection = ref state.Projection;


        ImGui.BeginGroup();
        ImGui.TextUnformatted("Near / Far"u8);

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

        ImGui.TextUnformatted("Field of view"u8);
        ImGui.SliderFloat("##camera-fov", ref state.Projection.Fov, StateLimits.MinFov, StateLimits.MaxFov, "%.2f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.EndGroup();

        return fieldStatus.HasEdited(out _);
    }

    private static bool DrawTransform(ref EditorCameraState state)
    {
        var fieldStatus = new ImGuiFieldStatus();
        ref var t = ref state.Transform;

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Transform"u8);
        ImGui.Separator();

        ImGui.InputFloat3("##camera-translation", ref t.Translation, "%.3f");
        fieldStatus.NextField();

        ImGui.TextUnformatted("Rotation"u8);
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