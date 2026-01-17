using System.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Assets;

internal sealed class TexturePropertyUi(AssetsComponent component)
{
    public void Draw(TextureProxyProperty prop, ref FrameContext ctx)
    {
        ref var sw = ref ctx.Sw;
        var tex = prop.Asset;

        var layout = new TextLayout();
        var field = new FormFieldStatus();

        layout.TitleSeparator(sw.Write("Specifications"))
            .Property("Size:"u8, SpanWriterUtil.WriteSize(ref sw, tex.Size))
            .Property("Kind:"u8, tex.TextureKind.ToTextUtf8())
            .SameLineProperty()
            .Property("Format:"u8, tex.PixelFormat.ToTextUtf8())
            .Property("Mips:"u8, sw.Write(tex.MipLevels));

        layout.TitleSeparator(sw.Write("Sampler Settings"));

        var presetCombo = new EnumCombo<TexturePreset>((int)prop.Preset);
        if (presetCombo.Draw(ref sw, "Preset##tex-pre"u8, out var newPreset))
            component.TriggerTextureUpdate(prop, nameof(prop.Preset), (int)newPreset);

        var anisoCombo = new EnumCombo<AnisotropyLevel>((int)prop.Anisotropy);
        if (anisoCombo.Draw(ref sw, "Anisotropy##tex-aniso"u8, out var newAniso))
            component.TriggerTextureUpdate(prop, nameof(prop.Anisotropy), (int)newAniso);

        var usageCombo = new EnumCombo<TextureUsage>((int)tex.Usage);
        if (usageCombo.Draw(ref sw, "Usage##tex-usage"u8, out var newUsage))
            component.TriggerTextureUpdate(prop, nameof(prop.Usage), (int)newUsage);

        var formatCombo = new EnumCombo<TexturePixelFormat>((int)tex.PixelFormat);
        if (formatCombo.Draw(ref sw, "PixelFormat##tex-pixel"u8, out var newFormat))
            component.TriggerTextureUpdate(prop, nameof(prop.PixelFormat), (int)newFormat);

        layout.RowSpace();
        field.InputFloat("LOD"u8, "##lodlvl", ref prop.LodLevel);

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