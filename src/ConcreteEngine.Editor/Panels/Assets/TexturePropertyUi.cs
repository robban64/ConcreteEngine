using System.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Assets;

internal sealed class TexturePropertyUi(PanelContext panelContext)
{
    private readonly EnumCombo<TexturePreset> _presetCombo = new(label:"Preset");
    private readonly EnumCombo<AnisotropyLevel> _anisoCombo = new(label:"Anisotropy") ;
    private readonly EnumCombo<TextureUsage> _usageCombo = new(label:"Usage");
    private readonly EnumCombo<TexturePixelFormat> _formatCombo = new(start: 1, label: "Format");
/*
    public readonly FloatInputValueField<Float1Value> LodLevel;
    public readonly ComboField Preset;
    public readonly ComboField Anisotropy;
    public readonly ComboField Usage;
    public readonly ComboField PixelFormat;
*/
    public unsafe void Draw(TextureProxyProperty prop, in FrameContext ctx)
    {
        var sw = ctx.Writer;
        var tex = prop.Asset;

        var layout = new TextLayout();

        layout.TitleSeparator("Specifications"u8)
            .Property("Size:"u8, ref WriteFormat.WriteSize(sw, tex.Size))
            .Property("Kind:"u8, ref sw.Write(tex.TextureKind.ToText()))
            .SameLineProperty()
            .Property("Format:"u8, ref sw.Write(tex.PixelFormat.ToText()))
            .Property("Mips:"u8, ref sw.Write(tex.MipLevels))
            .TitleSeparator("Sampler Settings"u8);

        if (_presetCombo.Draw((int)prop.Preset, out var newPreset)) ;
        //TriggerTextureUpdate(prop, nameof(prop.Preset), (int)newPreset);

        if (_anisoCombo.Draw((int)prop.Anisotropy, out var newAniso)) ;
        //TriggerTextureUpdate(prop, nameof(prop.Anisotropy), (int)newAniso);

        if (_usageCombo.Draw((int)prop.Usage, out var newUsage)) ;
        //TriggerTextureUpdate(prop, nameof(prop.Usage), (int)newUsage);

        if (_formatCombo.Draw((int)prop.PixelFormat, out var newFormat)) ;
        //TriggerTextureUpdate(prop, nameof(prop.PixelFormat), (int)newFormat);

        layout.RowSpace();
        var field = new FormFieldInputs();
        field.InputFloat("LOD"u8, InputComponents.Float1, ref prop.LodLevel, "%.3");

        layout.RowSpace();

        if (ImGui.Button("Show Preview"u8, new Vector2(-1, 0)))
            ImGui.OpenPopup("##tex-prew-popup"u8);

        if (ImGui.BeginPopup("##tex-prew-popup"u8))
        {
            var texPtr = panelContext.GetTextureRefPtr(tex.GfxId);
            ImGui.Image(*texPtr.Handle, new Vector2(256, 256));
            
            if (ImGui.Button("Close"u8)) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }
}