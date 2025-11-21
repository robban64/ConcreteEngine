#region

using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Assets.Data;

public readonly record struct AssetTextureSlot(
    AssetId Asset,
    TextureSlotKind SlotKind,
    TextureKind TextureKind = TextureKind.Texture2D,
    TexturePixelFormat PixelFormat = TexturePixelFormat.SrgbAlpha
)
{
    public bool IsFallback => !Asset.IsValid;
}