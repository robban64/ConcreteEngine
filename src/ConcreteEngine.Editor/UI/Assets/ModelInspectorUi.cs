using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class ModelInspectorUi(StateContext panelContext)
{
    public void Draw(InspectModel editModel, FrameContext ctx)
    {
        var model = editModel.Asset;

        ImGui.SeparatorText("Model Info"u8);
        AppDraw.DrawTextProperty("Vertices:"u8, ctx.Sw.Write(model.Info.VertexCount));
        AppDraw.DrawTextProperty("Triangles:"u8, ctx.Sw.Write(model.Info.FaceCount));
        AppDraw.DrawTextProperty("Meshes:"u8, ctx.Sw.Write((int)model.Info.MeshCount));
        AppDraw.DrawTextProperty("Animated:"u8, WriteFormat.BoolToYesNoShort(model.Info.IsAnimated));


        ImGui.SeparatorText("Meshes"u8);
        foreach (var mesh in editModel.Asset.Meshes)
        {
            if (!ImGui.TreeNodeEx(ctx.Sw.Write(mesh.Name), ImGuiTreeNodeFlags.SpanFullWidth)) continue;

            var spec = mesh.Info;
            AppDraw.DrawTextProperty("Vertices:"u8, ctx.Sw.Write(spec.MeshIndex));
            AppDraw.DrawTextProperty("MatIndex:"u8, ctx.Sw.Write(spec.MaterialIndex));
            AppDraw.DrawTextProperty("Vertices:"u8, ctx.Sw.Write(spec.VertexCount));
            AppDraw.DrawTextProperty("Triangles:"u8, ctx.Sw.Write(spec.TrisCount));

            ImGui.TreePop();
        }

        if (model.Animation != null)
            DrawAnimated(model.Animation, ctx);
    }

    private static void DrawAnimated(ModelAnimation animation, FrameContext ctx)
    {
        var sw = ctx.Sw;
        ImGui.SeparatorText("Animation"u8);
        AppDraw.DrawTextProperty("Bone Count:"u8, sw.Write(animation.BoneCount));

        if (ImGui.BeginTable("##anim_table"u8, 4, GuiTheme.TableFlags))
        {
            ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Duration"u8, ImGuiTableColumnFlags.WidthFixed, 50f);
            ImGui.TableSetupColumn("TPS"u8, ImGuiTableColumnFlags.WidthFixed, 50f);
            ImGui.TableSetupColumn("Track"u8, ImGuiTableColumnFlags.WidthFixed, 36f);

            ImGui.TableHeadersRow();

            foreach (var clip in animation.Clips)
            {
                ImGui.TableNextRow();
                float rowHeight = AppDraw.ColumnV(sw.Write(clip.Name));
                AppDraw.ColumnV(sw.Write(clip.Duration), rowHeight);
                AppDraw.ColumnV(sw.Write(clip.TicksPerSecond), rowHeight);
                AppDraw.ColumnV(sw.Write(clip.Length), rowHeight);

            }

            ImGui.EndTable();
        }
    }
}