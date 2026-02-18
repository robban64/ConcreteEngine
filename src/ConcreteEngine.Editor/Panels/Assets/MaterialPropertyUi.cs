using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Assets;

internal sealed class MaterialPropertyUi(PanelContext panelContext)
{
    private readonly EnumCombo<BlendMode> _blendCombo = new(start: 1, label: "Blend Mode");
    private readonly EnumCombo<CullMode> _cullCombo = new(start: 2, label: "Cull Mode");
    private readonly EnumCombo<DepthMode> _depthCombo = new(start: 2, label: "Depth Mode");
    private readonly EnumCombo<PolygonOffsetLevel> _polygonCombo = new(start: 2, label: "Polygon Offset");

    public void DrawMaterialProperties(MaterialProxyProperty matProp, in FrameContext ctx)
    {
        var layout = new TextLayout();
        var sw = ctx.Writer;
        ImGui.BeginGroup();
        if (matProp.TemplateMaterial != null)
        {
            layout.PropertyColor(in StyleMap.GetAssetColor(AssetKind.Material), "Parent:"u8, ref sw.Write(matProp.TemplateMaterial.Name));
        }

        layout.PropertyColor(in StyleMap.GetAssetColor(AssetKind.Shader), "Shader:"u8, ref sw.Write(matProp.Shader.Name));
        ImGui.EndGroup();

        var prevPipeline = matProp.Pipeline;
        layout.TitleSeparator("Texture Slots"u8);
        DrawTextureSlots(matProp, sw);

        var changedParams = DrawParams(matProp);
        DrawPipeline(matProp, sw);

        var changedPipeline = matProp.Pipeline != prevPipeline;

        if (changedParams || changedPipeline)
        {
            matProp.Commit();
        }
    }

    private bool DrawParams(MaterialProxyProperty matProp)
    {
        ref var param = ref matProp.Params;
        var fields = FormFieldInputs.MakeVertical();
        ImGui.SeparatorText("Base Parameters"u8);
        fields.ColorEdit4("Color"u8, ref param.Color.R);
        fields.ToggleDefault();
        fields.InputFloat("Specular"u8, InputComponents.Float1, ref param.Specular, "%.3f");
        fields.InputFloat("Shininess"u8, InputComponents.Float1, ref param.Shininess, "%.3f");
        fields.InputFloat("UV Repeat"u8, InputComponents.Float1, ref param.UvRepeat, "%.3f");
        return fields.HasEdited(out _);
    }

    private void DrawPipeline(MaterialProxyProperty matProp, UnsafeSpanWriter sw)
    {
        ref var pipeline = ref matProp.Pipeline;
        ref var passState = ref pipeline.PassState;

        ImGui.SeparatorText("Pipeline State"u8);
        DrawFlagToggle(_blendCombo.Label, GfxStateFlags.Blend, ref passState, sw);
        DrawFlagToggle(_cullCombo.Label, GfxStateFlags.Cull, ref passState, sw);
        DrawFlagToggle("Depth Test"u8, GfxStateFlags.DepthTest, ref passState, sw);
        DrawFlagToggle("Depth Write"u8, GfxStateFlags.DepthWrite, ref passState, sw);
        DrawFlagToggle(_polygonCombo.Label, GfxStateFlags.PolygonOffset, ref passState, sw);

        ImGui.Separator();
        DrawPassFunctions(passState, ref pipeline.PassFunctions);
    }

    private void DrawPassFunctions(GfxPassState passState, ref GfxPassFunctions passFuncs)
    {
        if (passState.IsEmpty) return;
        ImGui.PushItemWidth(110);

        if (passState.IsSet(GfxStateFlags.Blend))
        {
            if (_blendCombo.Draw((int)passFuncs.Blend, out var newVal))
                passFuncs.Blend = newVal;
        }

        if (passState.IsSet(GfxStateFlags.Cull))
        {
            if (_cullCombo.Draw((int)passFuncs.Cull, out var newVal))
                passFuncs.Cull = newVal;
        }

        if (passState.IsSet(GfxStateFlags.DepthTest))
        {
            if (_depthCombo.Draw((int)passFuncs.Depth, out var newVal))
                passFuncs.Depth = newVal;
        }

        if (passState.IsSet(GfxStateFlags.PolygonOffset))
        {
            if (_polygonCombo.Draw((int)passFuncs.PolygonOffset, out var newVal))
                passFuncs.PolygonOffset = newVal;
        }

        ImGui.PopItemWidth();
    }


    private static void DrawFlagToggle(ReadOnlySpan<byte> label, GfxStateFlags flag, ref GfxPassState state,
        UnsafeSpanWriter sw)
    {
        var isDefined = state.IsSet(flag);
        if (ImGui.Checkbox(ref sw.Start(label).Append("##1-"u8).Append((int)flag).End(), ref isDefined))
            state = new GfxPassState(state.Enabled, state.Defined ^ flag);

        if (!isDefined) return;

        ImGui.SameLine(110);

        var isEnabled = state.IsSet(flag);
        if (ImGui.Checkbox(ref sw.Start("##2-"u8).Append((int)flag).End(), ref isEnabled))
            state = new GfxPassState(state.Enabled ^ flag, state.Defined);
    }

    private void DrawTextureSlots(MaterialProxyProperty matProp, UnsafeSpanWriter sw)
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
                                      ImGuiTableFlags.BordersInnerH;

        if (!ImGui.BeginTable("##mat_tex_table"u8, 2, flags)) return;

        var usageNames = EnumCache<TextureUsage>.Names;
        var textures = matProp.Textures;
        var bindings = matProp.Bindings;

        var len = textures.Length;
        if (len != bindings.Length) throw new IndexOutOfRangeException();

        var layout = TextLayout.Make()
            .Row("Label"u8, 0.35f, ImGuiTableColumnFlags.None).RowStretch("Slot"u8);

        for (int i = 0; i < len; i++)
        {
            var binding = bindings[i];
            var texture = textures[i];

            ImGui.PushID(i);
            ImGui.TableNextRow();
            layout.Column(ref sw.Write(usageNames[(int)binding.Usage]));
            DrawHover(binding, sw);

            ImGui.TableNextColumn();
            if (texture is not null)
                DrawAssetSlot(texture, sw);
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

    private unsafe void DrawAssetSlot(ITexture currentTex, UnsafeSpanWriter sw)
    {
        var rowHeight = ImGui.GetFrameHeight();

        var clearBtnWidth = rowHeight + ImGui.GetStyle().ItemSpacing.X;
        var contentWidth = ImGui.GetContentRegionAvail().X - clearBtnWidth;

        if (ImGui.Button(ref sw.Write(currentTex.Name), new Vector2(contentWidth, rowHeight)))
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
            var texPtr = panelContext.GetTextureRefPtr(currentTex.GfxId);
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