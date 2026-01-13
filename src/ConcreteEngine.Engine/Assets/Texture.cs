using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets;

public sealed record Texture : AssetObject, ITexture
{
    public required TextureId GfxId { get; init; }

    public required int Width { get; init; }
    public required int Height { get; init; }

    public required TexturePreset Preset { get; init; } = TexturePreset.LinearClamp;
    public required TextureKind TextureKind { get; init; } = TextureKind.Texture2D;
    public required TexturePixelFormat PixelFormat { get; init; } = TexturePixelFormat.SrgbAlpha;
    public required TextureAnisotropyProfile Anisotropy { get; init; } = TextureAnisotropyProfile.Off;

    public MaterialSlotKind SlotKind { get; init; }

    public override AssetCategory Category => AssetCategory.Graphic;
    public override AssetKind Kind => AssetKind.Texture;

    //TODO remove
    public ReadOnlyMemory<byte>? PixelData { get; private set; }
    public void SetPixelData(ReadOnlyMemory<byte> pixelData) => PixelData = pixelData;
}