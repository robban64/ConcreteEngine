using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components.Assets;

internal static class DrawAssets
{
    private static ReadOnlySpan<string> GetTextureUsageNames() => EnumCache<TextureUsage>.GetNames();

    public static void CategoryChanged(AssetState state, AssetKind kind)
    {
        if (kind == state.ShowKind) return;
        state.ShowKind = kind;

        if (kind == AssetKind.Unknown) state.ResetState();
    }

    public static void DrawAssetTypeSelector(AssetState state, int length, ref ZaUtf8SpanWriter za)
    {
        var category = state.ShowKind;

        var currentLabel = category.ToTextUtf8();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);

        za.Clear();
        if (ImGui.BeginCombo("##assetTypeSelector"u8, currentLabel, ImGuiComboFlags.HeightLargest))
        {
            for (var i = 0; i < length; i++)
            {
                var isSelected = i == (int)category;
                var kind = (AssetKind)i;

                if (ImGui.Selectable(kind.ToTextUtf8(), isSelected, ImGuiSelectableFlags.None, Vector2.Zero))
                    CategoryChanged(state, kind);

                if (isSelected)
                    ImGui.SetItemDefaultFocus();

                za.Clear();
            }

            ImGui.EndCombo();
        }
    }

    public static void DrawSelectedInfo(AssetProxy proxy, ref ZaUtf8SpanWriter za)
    {
        var asset = proxy.Asset;

        var text = za.Append(asset.Kind.ToTextUtf8()).Append(" ["u8).Append(asset.Id).AppendEnd("]"u8).AsSpan();
        ImGui.SeparatorText(text);
        za.Clear();

        ImGui.TextUnformatted("Name:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AppendEnd(asset.Name).AsSpan());
        za.Clear();

        ImGui.TextUnformatted("GID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.Append(proxy.GIdString).AsSpan());
        za.Clear();

        ImGui.TextUnformatted("Generation:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AppendEnd(asset.Generation).AsSpan());
        za.Clear();
    }

    public static void DrawMaterialProperties(MaterialProxyProperty matProp, ref ZaUtf8SpanWriter za)
    {
        za.Clear();

        ImGui.TextUnformatted("Shader:"u8);
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f),za.AppendEnd( matProp.Shader.Name).AsSpan());

        if (matProp.TemplateMaterial != null)
        {
            ImGui.TextUnformatted("Parent:"u8);
            ImGui.SameLine();
            ImGui.TextUnformatted(za.AppendEnd(matProp.TemplateMaterial.Name).AsSpan());
        }
        za.Clear();
        ImGui.Spacing();
        DrawTextureSlots(matProp, ref za);

        za.Clear();
    }

private static void DrawTextureSlots(MaterialProxyProperty matProp, ref ZaUtf8SpanWriter za)
{
    ImGui.SeparatorText("Texture Slots"u8);

    var usageSpan = GetTextureUsageNames();
    var textures = matProp.Textures;
    var bindings = matProp.Bindings;

    if (ImGui.BeginTable("##mat_tex_table"u8, 2, 
        ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH))
    {
        ImGui.TableSetupColumn("Label"u8, ImGuiTableColumnFlags.None, 0.35f);
        ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthStretch);

        for (int i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];
            var currentTex = textures[i];
            
            ImGui.PushID(i);
            ImGui.TableNextRow();
            
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding(); 
            
            ImGui.TextUnformatted(za.AppendEnd(usageSpan[(int)binding.Usage]).AsSpan());
            za.Clear();

            if (ImGui.IsItemHovered())
            {
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

            ImGui.TableNextColumn();

            DrawAssetSlot(currentTex, binding.IsFallback, ref za);

            ImGui.PopID();
        }

        ImGui.EndTable();
    }
}

private static void DrawAssetSlot(ITexture? currentTex, bool isFallback, ref ZaUtf8SpanWriter za)
{
    float rowHeight = ImGui.GetFrameHeight();
    float buttonHeight = rowHeight; 
    
    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.2f, 1.0f));
    ImGui.Button("##icon"u8, new Vector2(buttonHeight, buttonHeight));
    ImGui.PopStyleColor();
    
    if (ImGui.BeginDragDropTarget())
    {
        // var payload = ImGui.AcceptDragDropPayload("ASSET_TEXTURE"u8);
        ImGui.EndDragDropTarget();
    }

    ImGui.SameLine();

    bool hasTexture = currentTex != null;
    ReadOnlySpan<byte> btnText;

    if (currentTex != null)
    {
        btnText = za.AppendEnd(currentTex.Name).AsSpan();
    }
    else
    {
        btnText = isFallback ? "Missing Asset"u8 : "None (Select)"u8;
    }

    float clearBtnWidth = hasTexture ? buttonHeight + ImGui.GetStyle().ItemSpacing.X : 0;
    float contentWidth = ImGui.GetContentRegionAvail().X - clearBtnWidth;

    if (isFallback) 
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
    else if (!hasTexture) 
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));

    if (ImGui.Button(btnText, new Vector2(contentWidth, buttonHeight)))
    {
    }

    if (ImGui.BeginDragDropTarget())
    {
        ImGui.EndDragDropTarget();
    }

    if (isFallback || !hasTexture) ImGui.PopStyleColor();
    za.Clear();

    if (hasTexture)
    {
        ImGui.SameLine();
        if (ImGui.Button("X"u8, new Vector2(buttonHeight, buttonHeight)))
        {
        }
        if (ImGui.IsItemHovered()) SetTooltip("Clear Slot"u8);
    }
}

private static void SetTooltip(ReadOnlySpan<byte> text)
{
    ImGui.BeginTooltip();
    ImGui.TextUnformatted(text);
    ImGui.EndTooltip();
}
    public static void DrawAssetKindTag(AssetKind kind)
    {
        var color = kind switch
        {
            AssetKind.Shader => new Vector4(0.392f, 0.584f, 0.929f, 1.0f),
            AssetKind.Model => new Vector4(1f, 0.647f, 0f, 1.0f),
            AssetKind.Texture => new Vector4(0.4f, 0.4f, 0.8f, 1.0f),
            AssetKind.Material => new Vector4(0.4f, 0.8f, 0.4f, 1.0f),
            _ => Vector4.One
        };
        ImGui.TextColored(color, kind.ToShortTextUtf8());
    }
}