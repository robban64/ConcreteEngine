using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Inspector.Impl;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EngineObjectStore;

namespace ConcreteEngine.Editor.UI;

internal sealed  class CameraPanel(StateManager state) : EditorPanel(PanelId.Camera, state)
{
    private Size2D _currentViewport;
    private RangeU16 _viewportStrHandle;
    private RangeU16 _aspectStrHandle;
    
    private readonly InspectCameraFields _inspectFields = InspectorFieldProvider.Instance.CameraFields;

    private FloatInput<Float1Value> _input1 = new ("Float1", FieldWidgetKind.Input);
    private FloatInput<Float2Value> _input2 = new ("Float2", FieldWidgetKind.Input);
    private FloatInput<Float3Value> _input3 = new ("Float3", FieldWidgetKind.Input);
    private FloatInput<Float4Value> _input4 = new ("Float4", FieldWidgetKind.Input);

    private void UpdateText()
    {
        var viewport = Camera.Viewport;
        
        DataPtr.Slice(_viewportStrHandle).Writer()
            .Append("Width: "u8).Append(viewport.Width)
            .Append(" - Height: "u8).Append(viewport.Height).End();

        DataPtr.Slice(_aspectStrHandle).Writer()
            .Append("Aspect Ratio: "u8).Append(viewport.AspectRatio, "F2").End();
    }

    public override void OnCreate()
    {
    }

    public override void OnEnter(ref MemoryBlockPtr memory)
    {
        _viewportStrHandle = memory.AllocSlice(32).AsRange16();
        _aspectStrHandle = memory.AllocSlice(24).AsRange16();
        
        _currentViewport = Camera.Viewport;
        
        UpdateText();
        _inspectFields.Refresh();
    }

    public override void OnUpdateDiagnostic()
    {
        if (_currentViewport != Camera.Viewport) UpdateText();
    }

    private AvgFrameTimer avg;
    public override void OnDraw(FrameContext ctx)
    {
        ImGui.SeparatorText("Viewport"u8);
        AppDraw.Text(DataPtr.Slice(_viewportStrHandle));
        AppDraw.Text(DataPtr.Slice(_aspectStrHandle));
/*
        avg.BeginSample();
        _input1.Draw();
        _input2.Draw();
        _input3.Draw();
        _input4.Draw();
        if (avg.EndSample() > 60) avg.ResetAndPrint("input");
        
        */
        ImGui.Spacing();
        
        _inspectFields.Draw();
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