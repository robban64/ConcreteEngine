using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Assets;

internal sealed class MaterialPropertyUi
{
    private readonly EnumCombo<BlendMode> _blendCombo = new(start: 1) { Label = "Blend Mode" };
    private readonly EnumCombo<CullMode> _cullCombo = new(start: 2) { Label = "Cull Mode" };
    private readonly EnumCombo<DepthMode> _depthCombo = new(start: 2) { Label = "Depth Mode" };
    private readonly EnumCombo<PolygonOffsetLevel> _polygonCombo = new(start: 2) { Label = "Polygon Offset" };

    public void DrawMaterialProperties(MaterialProxyProperty matProp, ref FrameContext ctx)
    {
        var layout = new TextLayout();
        ImGui.BeginGroup();
        if (matProp.TemplateMaterial != null)
        {
            var color = AssetKind.Material.ToColor();
            layout.PropertyColor(in color, "Parent:"u8, ctx.Sw.Write(matProp.TemplateMaterial.Name));
        }

        layout.PropertyColor(AssetKind.Shader.ToColor(), "Shader:"u8, ctx.Sw.Write(matProp.Shader.Name));
        ImGui.EndGroup();

        layout.TitleSeparator("Texture Slots"u8);
        DrawTextureSlots(matProp, ref ctx);
        DrawParams(matProp);
        DrawPipeline(matProp, ref ctx);
    }

    private void DrawParams(MaterialProxyProperty matProp)
    {
        ref var param = ref matProp.Params;
        var fields = FormFieldInputs.MakeVertical();
        ImGui.SeparatorText("Base Parameters"u8);
        fields.ColorEdit4("Color"u8, ref param.Color.R);
        fields.ToggleDefault();
        fields.InputFloat("Specular"u8, InputComponents.Float1, ref param.Specular, "%.3f");
        fields.InputFloat("Shininess"u8, InputComponents.Float1, ref param.Shininess, "%.3f");
        fields.InputFloat("UV Repeat"u8, InputComponents.Float1, ref param.UvRepeat, "%.3f");
        if (fields.HasEdited(out _)) { }
    }

    private void DrawPipeline(MaterialProxyProperty matProp, ref FrameContext ctx)
    {
        ref var pipeline = ref matProp.Pipeline;
        ref var passState = ref pipeline.PassState;

        ImGui.SeparatorText("Pipeline State"u8);
        DrawFlagToggle("Blending"u8, GfxStateFlags.Blend, ref passState, ref ctx);
        DrawFlagToggle("Culling"u8, GfxStateFlags.Cull, ref passState, ref ctx);
        DrawFlagToggle("Depth Test"u8, GfxStateFlags.DepthTest, ref passState, ref ctx);
        DrawFlagToggle("Depth Write"u8, GfxStateFlags.DepthWrite, ref passState, ref ctx);
        DrawFlagToggle("Polygon Offset"u8, GfxStateFlags.PolygonOffset, ref passState, ref ctx);

        ImGui.Separator();
        DrawPassFunctions(passState, ref pipeline.PassFunctions);
    }

    private void DrawPassFunctions(GfxPassState passState, ref GfxPassFunctions passFuncs)
    {
        if (passState.IsEmpty) return;
        ImGui.PushItemWidth(110);

        if (passState.IsSet(GfxStateFlags.Blend))
        {
            if (_blendCombo.Draw((int)passFuncs.Blend, "Empty", out var newVal))
                passFuncs.Blend = newVal;
        }

        if (passState.IsSet(GfxStateFlags.Cull))
        {
            if (_cullCombo.Draw((int)passFuncs.Cull, "Empty", out var newVal))
                passFuncs.Cull = newVal;
        }

        if (passState.IsSet(GfxStateFlags.DepthTest))
        {
            if (_depthCombo.Draw((int)passFuncs.Depth, "Empty", out var newVal))
                passFuncs.Depth = newVal;
        }

        if (passState.IsSet(GfxStateFlags.PolygonOffset))
        {
            if (_polygonCombo.Draw((int)passFuncs.PolygonOffset, "Empty", out var newVal))
                passFuncs.PolygonOffset = newVal;
        }

        ImGui.PopItemWidth();
    }


    private static void DrawFlagToggle(ReadOnlySpan<byte> label, GfxStateFlags flag, ref GfxPassState state,
        ref FrameContext ctx)
    {
        var isDefined = state.IsSet(flag);

        if (ImGui.Checkbox(ctx.Sw.Start(label).Append("##2-"u8).Append((int)flag).End(), ref isDefined))
            state = new GfxPassState(state.Enabled, state.Defined ^ flag);

        if (!isDefined) return;

        ImGui.SameLine(110);
        var isEnabled = state.IsSet(flag);
        if (ImGui.Checkbox(ctx.Sw.Start("##2-"u8).Append((int)flag).End(), ref isEnabled))
            state = new GfxPassState(state.Enabled ^ flag, state.Defined);
    }

    private static void DrawTextureSlots(MaterialProxyProperty matProp, ref FrameContext ctx)
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
                                      ImGuiTableFlags.BordersInnerH;

        if (!ImGui.BeginTable("##mat_tex_table"u8, 2, flags)) return;

        var usageSpan = EnumCache<TextureUsage>.GetNames();
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
            layout.Column(ctx.Sw.Write(usageSpan[(int)binding.Usage]));
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

    private static void DrawAssetSlot(ITexture currentTex, ref FrameContext ctx)
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