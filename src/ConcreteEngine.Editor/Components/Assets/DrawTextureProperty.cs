using System.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Assets;

internal sealed class DrawTextureProperty(AssetsComponent component)
{
    public void Draw(TextureProxyProperty prop, ref FrameContext ctx)
    {
        ref var sw = ref ctx.Sw;
        var tex = prop.Asset;

        var layout = new TextLayout();
        var field = new FormFieldStatus();
        float lod = tex.LodBias;

        //.Property("Mips:"u8, sw.Write(prop.MipCount));
        layout.LineSeperator("Specifications"u8)
            .Property("Size:"u8, TextHelper.WriteSize(ref sw, tex.Size))
            .Property("Kind:"u8, tex.TextureKind.ToTextUtf8())
            .SameLineProperty()
            .Property("Format:"u8, tex.PixelFormat.ToTextUtf8())
            .Property("Mips:"u8, sw.Write(tex.MipLevels));

        layout.LineSeperator("Sampler Settings"u8);

        field.InputFloat("LOD:"u8, "##lodlvl", ref lod);
            
        var presetCombo = new EnumCombo<TexturePreset>((int)tex.Preset);
        if (presetCombo.Draw("Preset"u8, "Select Preset..."u8, out var newPreset, ref sw))
            component.TriggerTextureUpdate(prop, nameof(tex.Preset), (int)newPreset);

        var anisoCombo = new EnumCombo<TextureAnisotropyProfile>((int)tex.Anisotropy);
        if (anisoCombo.Draw("Anisotropy"u8, "Profile"u8, out var newAniso, ref sw))
            component.TriggerTextureUpdate(prop, nameof(tex.Anisotropy), (int)newAniso);

        layout.RowSpace();

        if (ImGui.Button("Show Preview"u8, new Vector2(-1, 0)))
            ImGui.OpenPopup("TexturePreviewPopup"u8);

        if (ImGui.BeginPopup("TexturePreviewPopup"u8))
        {
            //ImGui.Image(id, new Vector2(256, 256));
            if (ImGui.Button("Close"u8)) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }
}