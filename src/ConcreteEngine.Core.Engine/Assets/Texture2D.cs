using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed record Texture2D : AssetObject
{
    public required TextureId ResourceId { get; init; }

    public required int Width { get; init; }
    public required int Height { get; init; }

    public MaterialSlotKind SlotKind { get; init; }

    public override AssetCategory Category => AssetCategory.Graphic;
    public override AssetKind Kind => AssetKind.Texture;
    public GraphicsKind GraphicsKind => GraphicsKind.Texture;

    //TODO remove
    public ReadOnlyMemory<byte>? PixelData { get; private set; }
    public void SetPixelData(ReadOnlyMemory<byte> pixelData) => PixelData = pixelData;
}