using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Inspector;

internal sealed unsafe class ModelInspectorUi(PanelContext panelContext, AssetController assetController)
{
    public void Draw(InspectModel editModel, FrameContext ctx)
    {
        var model = editModel.Asset;

        ImGui.SeparatorText("Model Info"u8);
        AppDraw.DrawTextProperty("Vertices:"u8, ctx.Write(model.Info.VertexCount));
        AppDraw.DrawTextProperty("Triangles:"u8, ctx.Write(model.Info.FaceCount));
        AppDraw.DrawTextProperty("Meshes:"u8, ctx.Write((int)model.Info.MeshCount));
        AppDraw.DrawTextProperty("Animated:"u8, WriteFormat.BoolToYesNoShort(model.Info.IsAnimated));


        ImGui.SeparatorText("Meshes"u8);
        foreach (var mesh in editModel.Asset.Meshes)
        {
            if (!ImGui.TreeNodeEx(ctx.Write(mesh.Name), ImGuiTreeNodeFlags.SpanFullWidth)) continue;

            var spec = mesh.Info;
            AppDraw.DrawTextProperty("Vertices:"u8, ctx.Write(spec.MeshIndex));
            AppDraw.DrawTextProperty("MatIndex:"u8, ctx.Write(spec.MaterialIndex));
            AppDraw.DrawTextProperty("Vertices:"u8, ctx.Write(spec.VertexCount));
            AppDraw.DrawTextProperty("Triangles:"u8, ctx.Write(spec.TrisCount));

            ImGui.TreePop();
        }

        if (model.Animation != null)
            DrawAnimated(model.Animation, ctx);
    }

    private static void DrawAnimated(ModelAnimation animation, FrameContext ctx)
    {
        ImGui.SeparatorText("Animation"u8);
        AppDraw.DrawTextProperty("Bone Count:"u8, ctx.Write(animation.BoneCount));

        if (ImGui.BeginTable("##anim_table"u8, 4, GuiTheme.TableFlags))
        {
            var layout = new TableLayout();
            layout.RowStretch("Name"u8).Row("Duration"u8, 50f).Row("TPS"u8, 50f).Row("Track"u8, 36f);
            ImGui.TableHeadersRow();

            layout.WithLayout(TextAlignMode.VerticalCenter);
            foreach (var clip in animation.Clips)
            {
                ImGui.TableNextRow();
                layout.Column(ctx.Write(clip.Name)).Column(ctx.Write(clip.Duration))
                    .Column(ctx.Write(clip.TicksPerSecond)).Column(ctx.Write(clip.Length));
            }

            ImGui.EndTable();
        }
    }
}