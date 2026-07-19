using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Inspectors;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Assets;

internal sealed class TextureInspectorUi(StateManager state)
{
    private readonly TextureInspector _inspector = new();

    public void Draw(Texture texture)
    {
        var sw = ScratchBuffer.Writer();

        ImGui.SeparatorText("Texture Info"u8);

        AppDraw.TextProperty("Dimension:"u8, sw.Write(texture.Size));

        AppDraw.TextProperty("InMemory:"u8, texture.HasPixelData ? "Yes"u8 : "No"u8);

        ImGui.SeparatorText("GPU Metadata"u8);

        var meta = GfxResourceApi.GetMeta(texture.GfxId);
        AppDraw.TextProperty("Kind:"u8, sw.Write(meta.Kind.ToText()));
        AppDraw.SameLineProperty();
        AppDraw.TextProperty("Format:"u8, sw.Write(meta.PixelFormat.ToText()));
        AppDraw.TextProperty("Mips:"u8, sw.Write(meta.MipLevels));

        _inspector.Draw();

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