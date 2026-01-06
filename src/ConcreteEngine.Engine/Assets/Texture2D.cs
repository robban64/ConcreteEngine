using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Assets;

public sealed record Texture2D : AssetObject
{
    public AssetRef<Texture2D> RefId => new(Id);

    public required TextureId ResourceId { get; init; }

    public required int Width { get; init; }
    public required int Height { get; init; }

    public TextureSlotKind SlotKind { get; init; }

    public override AssetCategory Category => AssetCategory.Graphic;
    public override AssetKind Kind => AssetKind.Texture;
    public GraphicsKind GraphicsKind => GraphicsKind.Texture;

    //TODO remove
    public ReadOnlyMemory<byte>? PixelData { get; private set; }
    public void SetPixelData(ReadOnlyMemory<byte> pixelData) => PixelData = pixelData;
}