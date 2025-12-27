using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Assets.Textures;

public sealed class Texture2D : AssetObject
{
    public AssetRef<Texture2D> RefId => new(RawId);

    public required TextureId ResourceId { get; init; }

    public required int Width { get; init; }
    public required int Height { get; init; }

    public TextureSlotKind SlotKind { get; init; }

    public override AssetCategory Category => AssetCategory.Graphic;
    public override AssetKind Kind => AssetKind.Texture2D;
    public GraphicsHandleKind GraphicsKind => GraphicsHandleKind.Texture;

    private byte[]? _pixelData;
    public ReadOnlyMemory<byte>? PixelData => _pixelData?.AsMemory();
    internal void SetPixelData(byte[] pixelData) => _pixelData = pixelData;
}