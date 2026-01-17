using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components;

internal sealed class SkyComponent
{
    public void DrawSkyboxProperties(AssetProxy proxy, TextureProxyProperty texProp, ref FrameContext ctx)
    {
        ref var sw = ref ctx.Sw;
        var asset = texProp.Asset;
        var filespecs = proxy.FileSpecs;

        var layout = new TextLayout();

        ImGui.SeparatorText("Environment Map (Cubemap)"u8);
        layout.Property("Resolution:"u8, TextHelper.WriteSize(ref sw, asset.Size))
            .Property("Format:"u8, asset.PixelFormat.ToTextUtf8())
            .Property("Faces:"u8, sw.Write(filespecs.Length));

        ImGui.Spacing();
        if (ImGui.BeginTable("##cubemap_faces"u8, 2, GuiTheme.TableFlags))
        {
            ImGui.TableSetupColumn("Face"u8, ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn("Source File"u8, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            for (int i = 0; i < filespecs.Length; i++)
            {
                var file = filespecs[i];
                ImGui.TableNextRow();
            
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(sw.Write(GetFaceName(i))); 

                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(sw.Write(file.RelativePath));
            }
            ImGui.EndTable();
        }

        ImGui.Spacing();

        if (ImGui.Button("Reload Cubemap"u8, new Vector2(-1, 0)))
        {
            //TriggerEvent(EventKey.SelectionAction, texProp.Asset.Name);
        }
    }

    private static string GetFaceName(int index) => index switch
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