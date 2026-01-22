using System.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Assets;

internal sealed class TexturePropertyUi()
{
    private readonly EnumCombo<TexturePreset> _presetCombo = new() { Label = "Preset" };
    private readonly EnumCombo<AnisotropyLevel> _anisoCombo = new() { Label = "Anisotropy" };
    private readonly EnumCombo<TextureUsage> _usageCombo = new() { Label = "Usage" };
    private readonly EnumCombo<TexturePixelFormat> _formatCombo = new(start: 1) { Label = "Format" };


    public void Draw(TextureProxyProperty prop, ref FrameContext ctx)
    {
        ref var sw = ref ctx.Sw;
        var tex = prop.Asset;

        var layout = new TextLayout();

        layout.TitleSeparator(sw.Write("Specifications"))
            .Property("Size:"u8, SpanWriterUtil.WriteSize(ref sw, tex.Size))
            .Property("Kind:"u8, tex.TextureKind.ToTextUtf8())
            .SameLineProperty()
            .Property("Format:"u8, tex.PixelFormat.ToTextUtf8())
            .Property("Mips:"u8, sw.Write(tex.MipLevels));

        layout.TitleSeparator(sw.Write("Sampler Settings"));

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

        if (ImGui.Button(sw.Write("Show Preview"), new Vector2(-1, 0)))
            ImGui.OpenPopup(sw.Write("TexturePreviewPopup"));


        if (ImGui.BeginPopup(sw.Write("TexturePreviewPopup")))
        {
            //ImGui.Image(id, new Vector2(256, 256));
            if (ImGui.Button("Close"u8)) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }
}