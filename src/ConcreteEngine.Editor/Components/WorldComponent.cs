using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal sealed class WorldState
{
    public WorldSelection Selection;
    public readonly SlotState<EditorCameraState> CameraState = new();
}

internal sealed class WorldComponent: EditorComponent
{
    private readonly EnumTabBar<WorldSelection> _tabBar = new(1);
    public readonly WorldState State = new();

    public override void DrawRight(ref FrameContext ctx)
    {
        if (_tabBar.Draw(out var selection))
            State.Selection = selection;

        switch (State.Selection)
        {
            case WorldSelection.Camera: DrawCamera(ref ctx); break;
            case WorldSelection.Sky: break;
        }
    }

    private void DrawCamera(ref FrameContext ctx)
    {
        if (!ImGui.BeginChild("##camera-props"u8, ImGuiChildFlags.AlwaysUseWindowPadding)) return;

        ref var data = ref State.CameraState.Data;

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
            TriggerEvent(new WorldEvent(EventKey.CommitData, State));
    }


    private static void DrawViewport(Size2D viewport, ref FrameContext ctx)
    {
        ref var sw = ref ctx.Sw;
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

    public void DrawSkyboxProperties(AssetProxy proxy, TextureProxyProperty texProp, ref FrameContext ctx)
    {
        ref var sw = ref ctx.Sw;
        var asset = texProp.Asset;
        var filespecs = proxy.FileSpecs;

        var layout = new TextLayout();

        ImGui.SeparatorText("Environment Map (Cubemap)"u8);
        layout
            .TitleSeparator(sw.Write("Environment Map (Cubemap)"), Vector2.Zero)
            .Property("Resolution:"u8, SpanWriterUtil.WriteSize(ref sw, asset.Size))
            .Property("Format:"u8, asset.PixelFormat.ToTextUtf8())
            .Property("Faces:"u8, sw.Write(filespecs.Length));

        ImGui.Spacing();
        if (ImGui.BeginTable("##cubemap_faces"u8, 2, GuiTheme.TableFlags))
        {
            layout.Row("Face"u8, 80f).RowStretch("Source File"u8);
            ImGui.TableHeadersRow();

            for (int i = 0; i < filespecs.Length; i++)
            {
                var file = filespecs[i];
                ImGui.TableNextRow();
                layout.Column(sw.Write(GetFaceName(i))).Column(sw.Write(file.RelativePath));
            }

            ImGui.EndTable();
        }

        ImGui.Spacing();

        if (ImGui.Button("Reload Cubemap"u8, new Vector2(-1, 0)))
        {
            //TriggerEvent(EventKey.SelectionAction, texProp.Asset.Name);
        }
    }

    private static string GetFaceName(int index) =>
        index switch
        {
            0 => "Right (+X)",
            1 => "Left (-X)",
            2 => "Top (+Y)",
            3 => "Bottom (-Y)",
            4 => "Front (+Z)",
            5 => "Back (-Z)",
            _ => "Unknown",
        };
}