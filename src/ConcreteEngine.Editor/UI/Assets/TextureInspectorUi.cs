using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Inspector;
using ConcreteEngine.Editor.Inspector.Impl;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed class TextureInspectorUi(StateManager state)
{
    public readonly InspectTextureFields InspectFields = InspectorFieldProvider.Instance.TextureFields;


    public void Draw(InspectTexture editTexture)
    {
        var sw = TextBuffers.GetWriter();
        var texture = editTexture.Asset;

        ImGui.SeparatorText("Texture Info"u8);

        AppDraw.DrawTextProperty("Dimension:"u8,
            sw.Append(texture.Size.Width).Append('x').Append(texture.Size.Height).End());
        
        AppDraw.DrawTextProperty("InMemory:"u8, texture.HasPixelData ? "Yes"u8 : "No"u8);

        ImGui.SeparatorText("GPU Metadata"u8);
        
        AppDraw.DrawTextProperty("Kind:"u8, sw.Write(editTexture.GfxMeta.Kind.ToText()));
        AppDraw.DrawSameLineProperty();
        AppDraw.DrawTextProperty("Format:"u8, sw.Write(editTexture.GfxMeta.PixelFormat.ToText()));
        AppDraw.DrawTextProperty("Mips:"u8, sw.Write(editTexture.GfxMeta.MipLevels));

        InspectFields.Draw();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Show Preview"u8, new Vector2(-1, 0)))
            ImGui.OpenPopup("##image-popup"u8);

        if (ImGui.BeginPopup("##image-popup"u8))
        {
            state.GetOrSetTextureHandle(texture.GfxId, ref AssetInspectorPanel.PopupTextureHandle);
            ImGui.Image(AssetInspectorPanel.PopupTextureHandle, new Vector2(256, 256));

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