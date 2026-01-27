using System.Numerics;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Proxy;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class WorldPanel(PanelContext context, WorldController worldController)
    : EditorPanel(PanelId.World, context)
{
    private WorldSelection _selection;
    private readonly SlotState<EditorCameraState> _cameraState = new();
    private readonly EnumTabBar<WorldSelection> _tabBar = new(0);

    public override void Update()
    {
        worldController.FetchCamera(_cameraState);
    }

    public override void Draw(in FrameContext ctx)
    {
        if (_tabBar.Draw(ctx.Writer, out var selection))
            _selection = selection;

        switch (_selection)
        {
            case WorldSelection.Camera: DrawCamera(in ctx); break;
            case WorldSelection.Sky: break;
        }
    }

    private void DrawCamera(in FrameContext ctx)
    {
        const float min = StateLimits.MinFov;
        const float max = StateLimits.MaxFov;

        ImGui.BeginChild("##camera-props"u8, ImGuiChildFlags.AlwaysUseWindowPadding);

        var layout = new TextLayout();
        ref var data = ref _cameraState.Data;

        ImGui.BeginGroup();
        {
            var sw = ctx.Writer;
            layout.TitleSeparator("Viewport"u8, padUp: false)
                .Property("Width:"u8, ref sw.Write(data.Viewport.Width))
                .SameLineProperty()
                .Property("Height:"u8, ref sw.Write(data.Viewport.Height))
                .Property("Aspect Ratio:"u8, ref sw.Write(data.Viewport.AspectRatio, "F2"));
        }
        ImGui.EndGroup();

        var fields = FormFieldInputs.MakeVertical();

        ImGui.BeginGroup();
        layout.TitleSeparator("Transform"u8);
        ref var trans = ref data.Transform;
        fields.InputFloat("Transform"u8, InputComponents.Float3, ref trans.Translation.X, "%.3f");
        fields.InputFloat("Rotation"u8, InputComponents.Float2, ref trans.Orientation.Yaw, "%.3f");
        ImGui.EndGroup();

        ImGui.BeginGroup();
        layout.TitleSeparator("Projection"u8);
        ref var proj = ref data.Projection;
        fields.InputFloat("Near"u8, InputComponents.Float2, ref proj.Near, "%.2f");
        fields.SliderFloat("Field of view"u8, InputComponents.Float1, ref proj.Fov, min, max, "%.2f");
        ImGui.EndGroup();

        ImGui.EndChild();

        if (fields.HasEdited(out _))
            Context.EnqueueEvent(new WorldEvent(_cameraState));
    }


    public void DrawSkyboxProperties(AssetObjectProxy proxy, TextureProxyProperty texProp, in FrameContext ctx)
    {
        var sw = ctx.Writer;
        var asset = texProp.Asset;
        var filespecs = proxy.FileSpecs;

        var layout = new TextLayout();

        layout
            .TitleSeparator("Environment Map (Cubemap)"u8)
            .Property("Resolution:"u8, ref WriteFormat.WriteSize(sw, asset.Size))
            .Property("Format:"u8, asset.PixelFormat.ToTextUtf8())
            .Property("Faces:"u8, ref sw.Write(filespecs.Length));

        ImGui.Spacing();
        if (ImGui.BeginTable("##cubemap_faces"u8, 2, GuiTheme.TableFlags))
        {
            layout.Row("Face"u8, 80f).RowStretch("Source File"u8);
            ImGui.TableHeadersRow();

            for (int i = 0; i < filespecs.Length; i++)
            {
                var file = filespecs[i];
                ImGui.TableNextRow();
                layout.Column(ref sw.Write(GetFaceName(i))).Column(ref sw.Write(file.RelativePath));
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