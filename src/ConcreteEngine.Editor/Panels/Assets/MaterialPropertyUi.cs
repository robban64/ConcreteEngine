using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Assets;

internal sealed class MaterialPropertyUi
{
    private readonly PanelContext _panelContext;
    private readonly AssetController _assetController;

    private EditorMaterial? GetMaterial()
    {
        if (_panelContext.SelectedAsset is EditorMaterial material)
            return material;

        return null;
    }

    public MaterialPropertyUi(PanelContext panelContext, AssetController assetController)
    {
        _panelContext = panelContext;
        _assetController = assetController;
    }

    public void DrawMaterialProperties(EditorMaterial material, in FrameContext ctx)
    {
        var layout = new TextLayout();
        var sw = ctx.Sw;
        ImGui.BeginGroup();
        if (material.Asset.TemplateId.IsValid())
        {
            var template = _assetController.GetAsset<Material>(material.Asset.TemplateId);
            ImGui.TextUnformatted("Template: "u8);
            ImGui.SameLine();
            ImGui.TextColored(StyleMap.GetAssetColor(AssetKind.Material), ref sw.Write(template.Name));
        }

        if (material.Asset.AssetShader.IsValid())
        {
            var shader = _assetController.GetAsset<Shader>(material.Asset.AssetShader);
            ImGui.TextUnformatted("Shader: "u8);
            ImGui.SameLine();
            ImGui.TextColored(StyleMap.GetAssetColor(AssetKind.Shader), ref sw.Write(shader.Name));
        }

        ImGui.EndGroup();

        //var prevPipeline = material.Pipeline;
        layout.TitleSeparator("Texture Slots"u8);
        DrawTextureSlots(material.Asset, sw);

        ImGui.SeparatorText("Base Parameters"u8);
        material.ColorField.DrawField(true);
        material.SpecularField.DrawField(false);
        material.ShininessField.DrawField(false);
        material.UvRepeatField.DrawField(false);

        DrawPipeline(material, sw);

        ImGui.Separator();
        DrawPassFunctions(material);

/*
        var changedPipeline = material.Pipeline != prevPipeline;

        if (changedParams || changedPipeline)
        {
            material.Commit();
        }
        */
    }

    private static void DrawPipeline(EditorMaterial material, UnsafeSpanWriter sw)
    {
        var passState = material.Asset.Pipeline.PassState;

        ImGui.SeparatorText("Pipeline State"u8);
        DrawFlagToggle("Blend Mode"u8, GfxStateFlags.Blend, ref passState, sw);
        DrawFlagToggle("Cull Mode"u8, GfxStateFlags.Cull, ref passState, sw);
        DrawFlagToggle("Depth Test"u8, GfxStateFlags.DepthTest, ref passState, sw);
        DrawFlagToggle("Depth Write"u8, GfxStateFlags.DepthWrite, ref passState, sw);
        DrawFlagToggle("Polygon Offset"u8, GfxStateFlags.PolygonOffset, ref passState, sw);

        if (material.Asset.Pipeline.PassState != passState)
            material.Asset.SetPassState(passState);

        return;

        static void DrawFlagToggle(ReadOnlySpan<byte> label, GfxStateFlags flag, ref GfxPassState state,
            UnsafeSpanWriter sw)
        {
            var isDefined = state.IsSet(flag);
            if (ImGui.Checkbox(ref sw.Start(label).Append("##1-"u8).Append((int)flag).End(), ref isDefined))
                state = new GfxPassState(state.Enabled, state.Defined ^ flag);

            if (!isDefined) return;

            ImGui.SameLine(130);

            var isEnabled = state.IsSet(flag);
            if (ImGui.Checkbox(ref sw.Start("##2-"u8).Append((int)flag).End(), ref isEnabled))
                state = new GfxPassState(state.Enabled ^ flag, state.Defined);
        }
    }

    private static void DrawPassFunctions(EditorMaterial material)
    {
        var passState = material.PassState;
        if (passState.IsEmpty) return;
        ImGui.PushItemWidth(110);

        if (passState.IsSet(GfxStateFlags.Blend))
            material.BlendCombo.DrawField();

        if (passState.IsSet(GfxStateFlags.Cull))
            material.CullCombo.DrawField();

        if (passState.IsSet(GfxStateFlags.DepthTest))
            material.DepthCombo.DrawField();

        if (passState.IsSet(GfxStateFlags.PolygonOffset))
            material.PolygonCombo.DrawField();

        ImGui.PopItemWidth();
    }


    private void DrawTextureSlots(Material asset, UnsafeSpanWriter sw)
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
                                      ImGuiTableFlags.BordersInnerH;

        if (!ImGui.BeginTable("##mat_tex_table"u8, 2, flags)) return;

        ImGui.TableSetupColumn("Label"u8, ImGuiTableColumnFlags.None, 0.35f);
        ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthStretch);

        var usageNames = EnumCache<TextureUsage>.Names;
        var bindings = asset.GetTextureSources();

        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];
            ImGui.PushID(i);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(ref sw.Write(usageNames[(int)binding.Usage]));

            DrawHover(binding, sw);

            ImGui.TableNextColumn();
            if (binding.Texture.IsValid())
                DrawAssetSlot(_assetController.GetAsset<Texture>(binding.Texture), sw);
            else
                DrawAssetSlotEmptyTexture(binding.IsFallback);

            ImGui.PopID();
        }

        ImGui.EndTable();
        return;

        static void DrawHover(TextureSource binding, UnsafeSpanWriter sw)
        {
            if (!ImGui.IsItemHovered()) return;

            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Binding Info"u8);
            ImGui.Separator();

            ImGui.TextUnformatted(ref sw.Start("Kind: "u8)
                .Append(binding.TextureKind.ToText())
                .Append("\nFormat: "u8)
                .Append(binding.PixelFormat.ToText())
                .End());

            ImGui.EndTooltip();
        }
    }


    private unsafe void DrawAssetSlot(Texture slotTexture, UnsafeSpanWriter sw)
    {
        var rowHeight = ImGui.GetFrameHeight();

        var clearBtnWidth = rowHeight + ImGui.GetStyle().ItemSpacing.X;
        var contentWidth = ImGui.GetContentRegionAvail().X - clearBtnWidth;

        if (ImGui.Button(ref sw.Write(slotTexture.Name), new Vector2(contentWidth, rowHeight)))
        {
            ImGui.OpenPopup("##mat-tex-prew-popup"u8);
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

        if (ImGui.BeginPopup("##mat-tex-prew-popup"u8))
        {
            var texPtr = _panelContext.GetTextureRefPtr(slotTexture.GfxId);
            ImGui.Image(*texPtr.Handle, new Vector2(256, 256));

            if (ImGui.Button("Close"u8)) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    private static void DrawAssetSlotEmptyTexture(bool isFallback)
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
ImGui.Button(""u8, new Vector2(buttonHeight, buttonHeight));
ImGui.PopStyleColor();

if (ImGui.BeginDragDropTarget())
{
    // var payload = ImGui.AcceptDragDropPayload("ASSET_TEXTURE"u8);
    ImGui.EndDragDropTarget();
}

ImGui.SameLine();
*/
}