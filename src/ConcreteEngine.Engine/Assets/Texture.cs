using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets;

public sealed record Texture : AssetObject, ITexture
{
    public required TextureId GfxId { get; init; }

    public required int Width { get; init; }
    public required int Height { get; init; }

    public MaterialSlotKind SlotKind { get; init; }

    public override AssetCategory Category => AssetCategory.Graphic;
    public override AssetKind Kind => AssetKind.Texture;

    //TODO remove
    public ReadOnlyMemory<byte>? PixelData { get; private set; }
    public void SetPixelData(ReadOnlyMemory<byte> pixelData) => PixelData = pixelData;
}