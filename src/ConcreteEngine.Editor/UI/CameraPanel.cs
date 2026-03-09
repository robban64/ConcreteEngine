using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjectStore;

namespace ConcreteEngine.Editor.UI;

internal sealed class CameraPanel(StateContext context) : EditorPanel(PanelId.Camera, context)
{
    private readonly FloatField<Float3Value> _translation = new("Translation", FieldWidgetKind.Input,
        static () => Camera.Translation,
        static value => Camera.Translation = (Vector3)value) { Format = "%.3f" };

    private readonly FloatField<Float2Value> _orientation = new("Orientation", FieldWidgetKind.Input,
        static () => (Vector2)Camera.Orientation,
        static value => Camera.Orientation = new YawPitch(value.X, value.Y)) { Format = "%.3f" };

    private readonly FloatField<Float2Value> _nearFar = new("Near/Far", FieldWidgetKind.Input,
        static () => new Float2Value(Camera.NearPlane, Camera.FarPlane),
        static value =>
        {
            Camera.NearPlane = value.X;
            Camera.FarPlane = value.Y;
        }) { Format = "%.2f", Delay = FieldGetDelay.High };

    private readonly FloatField<Float1Value> _fov = new("Field of view", FieldWidgetKind.Slider,
        static () => Camera.Fov,
        static value => Camera.Fov = value.X)
    {
        Format = "%.2f",
        Delay = FieldGetDelay.High,
        Layout = FieldLayout.Top,
        Min = 10f,
        Max = 179f
    };

    public override void Enter()
    {
        _translation.Refresh();
        _orientation.Refresh();
        _nearFar.Refresh();
        _fov.Refresh();
    }

    public override void Draw(FrameContext ctx)
    {
        var viewport = Camera.Viewport;

        ImGui.SeparatorText("Viewport"u8);
        ImGui.TextUnformatted(ref ctx.Sw.Append("Width: "u8).Append(viewport.Width).Append(" - Height: "u8)
            .Append(viewport.Height).End());
        ImGui.TextUnformatted(ref ctx.Sw.Append("Aspect Ratio: "u8).Append(viewport.AspectRatio, "F2").End());


        ImGui.Spacing();
        ImGui.SeparatorText("Transform"u8);
        _translation.Draw();
        _orientation.Draw();

        ImGui.Spacing();
        ImGui.SeparatorText("Projection"u8);
        _nearFar.Draw();
        _fov.Draw();
        
    }
/*
    public void DrawSkyboxProperties(Texture texture, FrameContext ctx)
    {
        var sw = ctx.Sw.Writer;
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