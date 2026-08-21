using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Assets;

internal sealed unsafe class ModelInspectorUi : Inspector<Model>
{
    public override InspectorId Id { get; }
    public override uint Icon { get; }

    public ModelInspectorUi()
    {
    }

    public override void Draw()
    {
        var sw = ScratchBuffer.Writer();

        ImGui.SeparatorText("Model Info"u8);
        var info = Target!.Info;
        AppDraw.TextProperty("Vertices:"u8, sw.Write(info.VertexCount));
        AppDraw.TextProperty("Triangles:"u8, sw.Write(info.FaceCount));
        AppDraw.TextProperty("Meshes:"u8, sw.Write((int)info.MeshCount));
        AppDraw.TextProperty("Animated:"u8, sw.Write(info.IsAnimated ? 'Y' : 'N'));


        ImGui.SeparatorText("Meshes"u8);
        foreach (var mesh in Target.GetMeshes())
        {
            if (!ImGui.TreeNodeEx(sw.Write(mesh.Name), ImGuiTreeNodeFlags.SpanFullWidth)) continue;

            var spec = mesh.Info;
            AppDraw.TextProperty("Vertices:"u8, sw.Write(spec.MeshIndex));
            AppDraw.TextProperty("MatIndex:"u8, sw.Write(spec.MaterialIndex));
            AppDraw.TextProperty("Vertices:"u8, sw.Write(spec.VertexCount));
            AppDraw.TextProperty("Triangles:"u8, sw.Write(spec.TrisCount));

            ImGui.TreePop();
        }

        if (Target.Rig != null)
            DrawAnimated(Target.Rig, sw);
    }

    private static void DrawAnimated(ModelRig rig, NativeSpanWriter sw)
    {
        ImGui.SeparatorText("Animation"u8);
        AppDraw.TextProperty("Bone Count:"u8, sw.Write(rig.BoneCount));

        if (ImGui.BeginTable("##anim_table"u8, 4, GuiTheme.TableFlags))
        {
            ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Duration"u8, ImGuiTableColumnFlags.WidthFixed, 50f);
            ImGui.TableSetupColumn("TPS"u8, ImGuiTableColumnFlags.WidthFixed, 50f);
            ImGui.TableSetupColumn("Track"u8, ImGuiTableColumnFlags.WidthFixed, 36f);

            ImGui.TableHeadersRow();

            for (int i = 0; i < rig.ClipCount; i++)
            {
                var clip = rig.GetClip(i);
                ImGui.TableNextRow();
                float rowHeight = AppDraw.ColumnV(sw.Write(clip.Name));
                AppDraw.ColumnV(sw.Write(clip.Duration), rowHeight);
                AppDraw.ColumnV(sw.Write(clip.TicksPerSecond), rowHeight);
                AppDraw.ColumnV(sw.Write(clip.ActiveChannelCount), rowHeight);
            }

            ImGui.EndTable();
        }
    }

}