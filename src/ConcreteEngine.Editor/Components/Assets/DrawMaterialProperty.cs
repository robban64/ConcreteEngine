using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Assets;

internal sealed class DrawMaterialProperty(AssetsComponent component)
{
    public void DrawMaterialProperties(MaterialProxyProperty matProp,ref FrameContext ctx)
    {
        var shaderColor = AssetKind.Shader.ToColor();

        ImGui.BeginGroup();
        DrawGui.DrawRightPropColor(ctx.Sw.Write(matProp.Shader.Name), "Shader:"u8, in shaderColor);
        ImGui.EndGroup();

        if (matProp.TemplateMaterial != null)
            DrawGui.DrawRightProp(ctx.Sw.Write(matProp.TemplateMaterial.Name), "Parent:"u8);

        ImGui.Spacing();
        ImGui.SeparatorText("Texture Slots"u8);
        DrawTextureSlots(matProp, ref ctx);
    }

    private void DrawTextureSlots(MaterialProxyProperty matProp,ref FrameContext ctx)
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
                                      ImGuiTableFlags.BordersInnerH;

        if (!ImGui.BeginTable("##mat_tex_table"u8, 2, flags)) return;

        var usageSpan = EnumCache<TextureUsage>.GetNames();
        var textures = matProp.Textures;
        var bindings = matProp.Bindings;

        var len = textures.Length;
        if (len != bindings.Length) throw new IndexOutOfRangeException();

        ImGui.TableSetupColumn("Label"u8, ImGuiTableColumnFlags.None, 0.35f);
        ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthStretch);

        var layout = new TextLayout();
        for (int i = 0; i < len; i++)
        {
            var binding = bindings[i];
            var texture = textures[i];

            ImGui.PushID(i);
            ImGui.TableNextRow();
            layout.NextColumn(ctx.Sw.Write(usageSpan[(int)binding.Usage]));
            DrawHover(binding, ref ctx);

            ImGui.TableNextColumn();
            if (texture is not null)
                DrawAssetSlot(texture, ref ctx);
            else
                DrawAssetSlotEmptyTexture(binding.IsFallback);

            ImGui.PopID();
        }

        ImGui.EndTable();
        return;

        static void DrawHover(TextureSource binding, ref FrameContext ctx)
        {
            if (!ImGui.IsItemHovered()) return;

            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Binding Info"u8);
            ImGui.Separator();

            var metaText = ctx.Sw.Start("Kind: "u8)
                .Append(binding.TextureKind.ToTextUtf8())
                .Append("\nFormat: "u8)
                .Append(binding.PixelFormat.ToTextUtf8())
                .End();

            ImGui.TextUnformatted(metaText);
            ImGui.EndTooltip();
        }
    }

    private void DrawAssetSlot(ITexture currentTex, ref FrameContext ctx)
    {
        var rowHeight = ImGui.GetFrameHeight();

        var clearBtnWidth = rowHeight + ImGui.GetStyle().ItemSpacing.X;
        var contentWidth = ImGui.GetContentRegionAvail().X - clearBtnWidth;

        if (ImGui.Button(ctx.Sw.Write(currentTex.Name), new Vector2(contentWidth, rowHeight)))
        {
        }

        ImGui.SameLine();
        if (ImGui.Button("X"u8, new Vector2(rowHeight, rowHeight)))
        {
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Clear Slot"u8);
            ImGui.EndTooltip();
        }
    }

    private void DrawAssetSlotEmptyTexture(bool isFallback)
    {
        var rowHeight = ImGui.GetFrameHeight();
        var contentWidth = ImGui.GetContentRegionAvail().X;

        ImGui.PushStyleColor(ImGuiCol.Text, isFallback ? Palette.RedBase : Palette.GrayBase);

        var text = isFallback ? "Missing Asset"u8 : "-"u8;
        if (ImGui.Button(text, new Vector2(contentWidth, rowHeight)))
        {
        }

        ImGui.PopStyleColor();
    }

    /*
ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.2f, 1.0f));
ImGui.Button("##icon"u8, new Vector2(buttonHeight, buttonHeight));
ImGui.PopStyleColor();

if (ImGui.BeginDragDropTarget())
{
    // var payload = ImGui.AcceptDragDropPayload("ASSET_TEXTURE"u8);
    ImGui.EndDragDropTarget();
}

ImGui.SameLine();
*/
}