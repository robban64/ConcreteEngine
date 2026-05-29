using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Inspector;
using ConcreteEngine.Editor.Inspector.Impl;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class MaterialInspectorUi(StateManager state)
{
    private static AssetStore Assets => EngineObjectStore.Assets;

    public readonly InspectMaterialFields InspectFields = InspectorFieldProvider.Instance.MaterialFields;

    public void Draw(InspectMaterial material)
    {
        var sw = TextBuffers.GetWriter();

        ImGui.SeparatorText("Material Info"u8);
        ImGui.BeginGroup();
        if (material.Asset.TemplateId.IsValid())
        {
            var template = EngineObjectStore.Assets.Get<Material>(material.Asset.TemplateId);
            ImGui.TextUnformatted("Template: "u8);
            ImGui.SameLine();
            //ImGui.TextColored(StyleMap.GetAssetColor(AssetKind.Material), sw.Write(template.Name));
            ImGui.TextColored(Color4.White, sw.Write(template.Name));
        }

        if (material.Asset.ShaderId.IsValid())
        {
            var shader = EngineObjectStore.Assets.Get<Shader>(material.Asset.ShaderId);
            ImGui.TextUnformatted("Shader: "u8);
            ImGui.SameLine();
            ImGui.TextColored(Color4.White, sw.Write(shader.Name));
            //ImGui.TextColored(StyleMap.GetAssetColor(AssetKind.Shader), sw.Write(shader.Name));
        }

        ImGui.EndGroup();

        ImGui.Spacing();
        ImGui.SeparatorText("Texture Slots"u8);
        DrawTextureSlots(material.Asset, sw);

        ImGui.SeparatorText("State Properties"u8);
        InspectFields.Draw(0, 1);

        ImGui.Spacing();
        DrawPipeline(material, sw);
    }

    private void DrawPipeline(InspectMaterial editMaterial, NativeSpanWriter sw)
    {
        var passState = editMaterial.DrawState;
        ImGui.SeparatorText("State Flag"u8);
        DrawFlagToggle("Blend Mode"u8, GfxDrawFlags.Blend, ref passState, sw);
        DrawFlagToggle("Cull Mode"u8, GfxDrawFlags.Cull, ref passState, sw);
        DrawFlagToggle("Depth Test"u8, GfxDrawFlags.DepthTest, ref passState, sw);
        DrawFlagToggle("Depth Write"u8, GfxDrawFlags.DepthWrite, ref passState, sw);
        DrawFlagToggle("Polygon Offset"u8, GfxDrawFlags.PolygonOffset, ref passState, sw);
        ImGui.Separator();
        DrawFlagToggle("A2C"u8, GfxDrawFlags.Ac2, ref passState, sw);

        if (editMaterial.DrawState != passState)
            editMaterial.Asset.SetPassState(passState);

        if (passState.IsEmpty()) return;

        ImGui.Spacing();
        ImGui.SeparatorText("State Value"u8);

        ImGui.PushItemWidth(110);

        if (passState.IsSet(GfxDrawFlags.Blend))
            InspectFields.BlendCombo.Draw();

        if (passState.IsSet(GfxDrawFlags.Cull))
            InspectFields.CullCombo.Draw();

        if (passState.IsSet(GfxDrawFlags.DepthTest))
            InspectFields.DepthCombo.Draw();

        if (passState.IsSet(GfxDrawFlags.PolygonOffset))
            InspectFields.PolygonCombo.Draw();

        ImGui.PopItemWidth();
    }

    private void DrawTextureSlots(Material asset, NativeSpanWriter sw)
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
            AppDraw.Text(sw.Append(usageNames[(int)binding.Usage]).End());

            DrawHover(binding, sw);

            ImGui.TableNextColumn();
            if (binding.AssetTexture.IsValid())
                DrawAssetSlot(asset, i, Assets.Get<Texture>(binding.AssetTexture), sw);
            else
                DrawAssetSlotEmptyTexture(asset, i, binding, sw);

            ImGui.PopID();
        }

        ImGui.EndTable();
        return;

        static void DrawHover(TextureSource binding, NativeSpanWriter sw)
        {
            if (!ImGui.IsItemHovered()) return;

            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Binding Info"u8);
            ImGui.Separator();

            AppDraw.Text(sw.Append("Kind: "u8)
                .Append(binding.TextureKind.ToText())
                .Append("\nFormat: "u8)
                .Append(binding.PixelFormat.ToText())
                .End());

            ImGui.EndTooltip();
        }
    }


    private void DrawAssetSlot(Material material, int slot, Texture slotTexture, NativeSpanWriter sw)
    {
        var rowHeight = ImGui.GetFrameHeight();
        var clearBtnWidth = rowHeight + ImGui.GetStyle().ItemSpacing.X;
        var contentWidth = ImGui.GetContentRegionAvail().X - clearBtnWidth;

        if (ImGui.Button(sw.Write(slotTexture.Name), new Vector2(contentWidth, rowHeight)))
            ImGui.OpenPopup("preview-popup"u8);

        DropTexture(material, slot);

        ImGui.SameLine();

        if (slotTexture.Id.IsValid() && ImGui.Button("X"u8, new Vector2(rowHeight, rowHeight)))
            material.SetTexture(slot, null);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Clear Slot"u8);
            ImGui.EndTooltip();
        }

        if (ImGui.BeginPopup("##preview-popup"u8))
        {
            state.GetOrSetTextureHandle(slotTexture.GfxId, ref AssetInspectorPanel.PopupTextureHandle);
            ImGui.Image(AssetInspectorPanel.PopupTextureHandle, new Vector2(256, 256));

            if (ImGui.Button("Close"u8)) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    private void DrawAssetSlotEmptyTexture(Material material, int slot, TextureSource source, NativeSpanWriter sw)
    {
        var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());

        if (source.IsFallback)
        {
            //ImGui.PushStyleColor(ImGuiCol.Text, Palette.GrayBase);
            ImGui.Button(sw.Write(source.GetFallbackName()), size);
            //ImGui.PopStyleColor();
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, Palette.OrangeBase);
        ImGui.Button("Empty Slot"u8, size);
        DropTexture(material, slot);

        ImGui.PopStyleColor();
    }

    private void DropTexture(Material material, int slot)
    {
        if (!ImGui.BeginDragDropTarget()) return;

        var payload = ImGui.AcceptDragDropPayload("ASSET_TEXTURE"u8);
        if (!payload.IsNull && payload.IsDelivery())
        {
            var droppedId = *(AssetId*)payload.Data;
            if (droppedId.Value > 0 && Assets.TryGet<Texture>(droppedId, out var droppedTex))
                material.SetTexture(slot, droppedTex);
        }

        ImGui.EndDragDropTarget();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawFlagToggle(ReadOnlySpan<byte> label, GfxDrawFlags flag, ref GfxDrawState state,
        NativeSpanWriter sw)
    {
        var isDefined = state.IsSet(flag);
        if (ImGui.Checkbox(sw.Append(label).Append("##1-"u8).Append((int)flag).End(), ref isDefined))
            state = new GfxDrawState(state.Enabled, state.Defined ^ flag);

        if (!isDefined) return;

        ImGui.SameLine(130);

        var isEnabled = state.IsEnabled(flag);
        if (ImGui.Checkbox(sw.Append("##2-"u8).Append((int)flag).End(), ref isEnabled))
            state = new GfxDrawState(state.Enabled ^ flag, state.Defined);
    }
}