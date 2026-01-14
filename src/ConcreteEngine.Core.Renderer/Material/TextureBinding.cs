using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Renderer.Material;

public readonly struct TextureBinding(TextureId texture, TextureUsage slotKind, TextureKind textureKind)
{
    public readonly TextureId Texture = texture;
    public readonly TextureUsage SlotKind = slotKind;
    public readonly TextureKind TextureKind = textureKind;
}