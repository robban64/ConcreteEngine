using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components.Assets;

internal sealed class DrawMaterialProperty(AssetsComponent component)
{
    public void DrawMaterialProperties(MaterialProxyProperty matProp, in FrameContext ctx)
    {
        var za = ctx.GetWriter();
        
        ImGui.TextUnformatted("Shader:"u8);
        ImGui.SameLine();
        ImGui.TextColored(AssetKind.Shader.ToColor(), za.AppendEnd(matProp.Shader.Name).AsSpan());
        za.Clear();

        if (matProp.TemplateMaterial != null)
        {
            ImGui.TextUnformatted("Parent:"u8);
            ImGui.SameLine();
            ImGui.TextUnformatted(za.AppendEnd(matProp.TemplateMaterial.Name).AsSpan());
            za.Clear();
        }

        ImGui.Spacing();
        DrawTextureSlots(matProp, ref za);
        za.Clear();
    }

    private void DrawTextureSlots(MaterialProxyProperty matProp, ref ZaUtf8SpanWriter za)
    {
        ImGui.SeparatorText("Texture Slots"u8);

        var usageSpan = EnumCache<TextureUsage>.GetNames();
        var textures = matProp.Textures;
        var bindings = matProp.Bindings;

        var len = textures.Length;
        if (len != bindings.Length) throw new IndexOutOfRangeException();

        if (!ImGui.BeginTable("##mat_tex_table"u8, 2,
                ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH))
        {
            return;
        }

        ImGui.TableSetupColumn("Label"u8, ImGuiTableColumnFlags.None, 0.35f);
        ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthStretch);

        for (int i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];

            ImGui.PushID(i);
            ImGui.TableNextRow();

            RefGui.DrawColumn(ref za.AppendEnd(usageSpan[(int)binding.Usage]));
            DrawHover(ref za, binding);

            ImGui.TableNextColumn();
            DrawAssetSlot(textures[i], binding.IsFallback, ref za);

            ImGui.PopID();
        }

        ImGui.EndTable();
        return;

        static void DrawHover(ref ZaUtf8SpanWriter za, TextureSource binding)
        {
            if (!ImGui.IsItemHovered()) return;

            ImGui.BeginTooltip();
            ImGui.Text("Binding Info"u8);
            ImGui.Separator();

            var metaText = za.Append("Kind: "u8)
                .Append(binding.TextureKind.ToTextUtf8())
                .Append("\nFormat: "u8)
                .AppendEnd(binding.PixelFormat.ToTextUtf8()).AsSpan();

            ImGui.TextUnformatted(metaText);
            za.Clear();
            ImGui.EndTooltip();
        }
    }

    private void DrawAssetSlot(ITexture? currentTex, bool isFallback, ref ZaUtf8SpanWriter za)
    {
        var rowHeight = ImGui.GetFrameHeight();
        var buttonHeight = rowHeight;

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
        var hasTexture = currentTex != null;
        ReadOnlySpan<byte> btnText;

        if (currentTex != null)
            btnText = za.AppendEnd(currentTex.Name).AsSpan();
        else
            btnText = isFallback ? "Missing Asset"u8 : "-"u8;

        var clearBtnWidth = hasTexture ? buttonHeight + ImGui.GetStyle().ItemSpacing.X : 0;
        var contentWidth = ImGui.GetContentRegionAvail().X - clearBtnWidth;

        if (isFallback)
            ImGui.PushStyleColor(ImGuiCol.Text, Palette.RedBase);
        else if (!hasTexture)
            ImGui.PushStyleColor(ImGuiCol.Text, Palette.GrayBase);

        if (ImGui.Button(btnText, new Vector2(contentWidth, buttonHeight)))
        {
        }

        if (isFallback || !hasTexture) ImGui.PopStyleColor();
        za.Clear();

        if (!hasTexture) return;

        ImGui.SameLine();
        if (ImGui.Button("X"u8, new Vector2(buttonHeight, buttonHeight)))
        {
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Clear Slot"u8);
            ImGui.EndTooltip();
        }
    }
}