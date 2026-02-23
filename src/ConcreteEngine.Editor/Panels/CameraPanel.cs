using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjects;

namespace ConcreteEngine.Editor.Panels;

internal sealed class CameraPanel(PanelContext context) : EditorPanel(PanelId.Camera, context)
{
    private readonly FloatInputValueField<Float3Value> _translation = new("Translation",
        static () => Camera.Translation,
        static value => Camera.Translation = (Vector3)value) { Format = "%.3f" };

    private readonly FloatInputValueField<Float2Value> _orientation = new("Orientation",
        static () => (Vector2)Camera.Orientation,
        static value => Camera.Orientation = new YawPitch(value.X, value.Y)) { Format = "%.3f" };

    private readonly FloatInputValueField<Float2Value> _nearFar = new("Near/Far",
        static () => new Float2Value(Camera.NearPlane, Camera.FarPlane),
        static value =>
        {
            Camera.NearPlane = value.X;
            Camera.FarPlane = value.Y;
        }) { Format = "%.2f", Delay = PropertyGetDelay.High };

    private readonly FloatSliderField<Float1Value> _fov = new("Field of view", 10f, 179f,
        static () => Camera.Fov,
        static value => Camera.Fov = value.X) { Format = "%.2f", Delay = PropertyGetDelay.High };

    public override void Enter()
    {
        _translation.Refresh();
        _orientation.Refresh();
        _nearFar.Refresh();
        _fov.Refresh();
    }

    public override void Draw(in FrameContext ctx)
    {
        var width = ImGui.GetContentRegionAvail().X;
        var viewport = Camera.Viewport;

        var sw = ctx.Sw;
        
        ImGui.SeparatorText("Viewport"u8);
        ImGui.TextUnformatted(ref sw.Start("Width: "u8).Append(viewport.Width).Append(" - Height: ")
            .Append(viewport.Height).End());
        ImGui.TextUnformatted(ref sw.Start("Aspect Ratio: "u8).Append(viewport.AspectRatio, "F2").End());

        ImGui.Spacing();
        ImGui.SeparatorText("Transform"u8);
        _translation.DrawField(true, width);
        _orientation.DrawField(true, width);

        ImGui.Spacing();
        ImGui.SeparatorText("Projection"u8);
        _nearFar.DrawField(true, width);
        _fov.DrawField(true, width);

    }
/*
    public void DrawSkyboxProperties(Texture texture, in FrameContext ctx)
    {
        var sw = ctx.Writer;
        var filespecs = proxy.FileSpecs;

        var layout = new TextLayout();

        layout
            .TitleSeparator("Environment Map (Cubemap)"u8)
            .Property("Resolution:"u8, ref WriteFormat.WriteSize(sw, texture.Size))
            .Property("Format:"u8, ref sw.Write(texture.PixelFormat.ToText()))
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
        */
}