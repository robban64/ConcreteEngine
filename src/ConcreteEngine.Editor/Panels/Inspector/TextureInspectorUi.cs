using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Inspector;

internal sealed class TextureInspectorUi(PanelContext panelContext, AssetController assetController)
{
    public unsafe void Draw(InspectTexture editTexture, FrameContext ctx)
    {
        var texture = editTexture.Asset;

        ImGui.SeparatorText("Texture Info"u8);
        AppDraw.DrawTextProperty("Size:"u8, ref WriteFormat.WriteSize(ctx.Sw, texture.Size));

        AppDraw.DrawTextProperty("Kind:"u8, ctx.Write(texture.TextureKind.ToText()));
        AppDraw.DrawSameLineProperty();
        AppDraw.DrawTextProperty("Format:"u8, ctx.Write(texture.PixelFormat.ToText()));

        AppDraw.DrawTextProperty("Mips:"u8, ctx.Write(texture.MipLevels));

        ImGui.SeparatorText("Texture Data"u8);
        editTexture.PixelFormat.DrawField(false);

        ImGui.Separator();

        ImGui.SeparatorText("Texture State"u8);
        editTexture.Preset.DrawField(false);
        editTexture.Anisotropy.DrawField(false);
        editTexture.Usage.DrawField(false);

        ImGui.Separator();
        editTexture.LodBias.DrawField(true);
        ImGui.Separator();

        if (ImGui.Button("Show Preview"u8, new Vector2(-1, 0)))
            ImGui.OpenPopup("##image-popup"u8);

        if (ImGui.BeginPopup("##image-popup"u8))
        {
            var texPtr = panelContext.GetTextureRefPtr(texture.GfxId);
            ImGui.Image(*texPtr.Handle, new Vector2(256, 256));

            if (ImGui.Button("Close"u8)) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
/*

        var layout = new TextLayout();

        layout.TitleSeparator("Specifications"u8)
            .Property("Size:"u8, ref WriteFormat.WriteSize(sw, texture.Size))
            .Property("Kind:"u8, ref sw.Write(texture.TextureKind.ToText()))
            .SameLineProperty()
            .Property("Format:"u8, ref sw.Write(texture.PixelFormat.ToText()))
            .Property("Mips:"u8, ref sw.Write(texture.MipLevels))
            .TitleSeparator("Sampler Settings"u8);


        if (_presetCombo.Draw((int)texture.Preset, out var newPreset)) ;
        //TriggerTextureUpdate(prop, nameof(prop.Preset), (int)newPreset);

        if (_anisoCombo.Draw((int)texture.Anisotropy, out var newAniso)) ;
        //TriggerTextureUpdate(prop, nameof(prop.Anisotropy), (int)newAniso);

        if (_usageCombo.Draw((int)texture.Usage, out var newUsage)) ;
        //TriggerTextureUpdate(prop, nameof(prop.Usage), (int)newUsage);

        if (_formatCombo.Draw((int)texture.PixelFormat, out var newFormat)) ;
        //TriggerTextureUpdate(prop, nameof(prop.PixelFormat), (int)newFormat);

        layout.RowSpace();
        var field = new FormFieldInputs();
        var lodBias = texture.LodBias;
        field.InputFloat("LOD"u8, InputComponents.Float1, ref lodBias, "%.3");

        layout.RowSpace();

        if (ImGui.Button("Show Preview"u8, new Vector2(-1, 0)))
            ImGui.OpenPopup("##image-popup"u8);

        if (ImGui.BeginPopup("##image-popup"u8))
        {
            var texPtr = panelContext.GetTextureRefPtr(texture.GfxId);
            ImGui.Image(*texPtr.Handle, new Vector2(256, 256));

            if (ImGui.Button("Close"u8)) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
*/
    }
}