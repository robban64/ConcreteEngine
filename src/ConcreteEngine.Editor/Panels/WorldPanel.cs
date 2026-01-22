using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Panels.State;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class WorldPanel() : EditorPanel(PanelId.World)
{
    private WorldSelection _selection;
    private readonly SlotState<EditorCameraState> _cameraState = new();
    private readonly EnumTabBar<WorldSelection> _tabBar = new(0);

    public override void Update()
    {
        EngineController.WorldController.FetchCamera(_cameraState.GetView());
    }

    public override void Draw(ref FrameContext ctx)
    {
        if (_tabBar.Draw(out var selection))
            _selection = selection;

        switch (_selection)
        {
            case WorldSelection.Camera: DrawCamera(ref ctx); break;
            case WorldSelection.Sky: break;
        }
    }

    private void DrawCamera(ref FrameContext ctx)
    {
        const float min = StateLimits.MinFov;
        const float max = StateLimits.MaxFov;

        if (!ImGui.BeginChild("##camera-props"u8, ImGuiChildFlags.AlwaysUseWindowPadding)) return;

        ref var data = ref _cameraState.Data;

        ImGui.SeparatorText("Viewport"u8);
        DrawViewport(data.Viewport, ref ctx);
        ImGui.Dummy(new Vector2(0, 2));

        ref var trans = ref data.Transform;
        ref var proj = ref data.Projection;

        var fields = FormFieldInputs.MakeVertical();

        ImGui.SeparatorText("Transform"u8);
        ImGui.BeginGroup();
        fields.InputFloat("Transform"u8, InputComponents.Float3, ref trans.Translation.X, "%.3f");
        fields.InputFloat("Rotation"u8, InputComponents.Float2, ref trans.Orientation.Yaw, "%.3f");
        ImGui.EndGroup();
        ImGui.Dummy(new Vector2(0, 2));

        ImGui.SeparatorText("Projection"u8);
        ImGui.BeginGroup();
        fields.InputFloat("Near"u8, InputComponents.Float2, ref proj.Near, "%.2f");
        fields.SliderFloat("Field of view"u8, InputComponents.Float1, ref proj.Fov, min, max, "%.2f");
        ImGui.EndGroup();

        ImGui.EndChild();

        if (fields.HasEdited(out _))
            Context.EnqueueEvent(new WorldEvent(_cameraState));
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

    public void DrawSkyboxProperties(AssetObjectProxy proxy, TextureProxyProperty texProp, ref FrameContext ctx)
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