using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class WorldPanel(PanelContext context, WorldController worldController)
    : EditorPanel(PanelId.World, context)
{
    private WorldSelection _selection;
    private readonly SlotState<EditorCameraState> _cameraState = new();
    private readonly EnumTabBar<WorldSelection> _tabBar = new(0);
    private readonly EditorCameraProperties _camera = worldController.GetEditorCameraProperties();

    public override void Enter()
    {
        _camera.Viewport.Refresh();
        _camera.Translation.Refresh();
        _camera.Orientation.Refresh();
        _camera.NearFar.Refresh();
        _camera.Fov.Refresh();
        
    }

    public override void Draw(in FrameContext ctx)
    {
        if (_tabBar.Draw(out var selection))
            _selection = selection;

        switch (_selection)
        {
            case WorldSelection.Camera: DrawCamera(); break;
            case WorldSelection.Sky: break;
        }

    }

    private void DrawCamera()
    {
        var layout = new TextLayout();

        ImGui.BeginChild("##camera-props"u8, ImGuiChildFlags.AlwaysUseWindowPadding);

        var width = ImGui.GetContentRegionAvail().X;

        ImGui.SeparatorText("Viewport"u8);
        _camera.Viewport.Draw();
        
        ImGui.Spacing();
        ImGui.SeparatorText("Transform"u8);
        _camera.Translation.Draw(true, width);
        _camera.Orientation.Draw(true, width);
        
        ImGui.Spacing();
        ImGui.SeparatorText("Projection"u8);
        _camera.NearFar.Draw(true, width);
        _camera.Fov.Draw(true, width);

        ImGui.EndChild();
        /*
        if (fields.HasEdited(out _))
            Context.EnqueueEvent(new WorldEvent(_cameraState));
        */
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