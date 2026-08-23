using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets;

[Inspect]
public sealed class Texture(
    string name,
    AssetId id,
    Guid gid,
    TextureId gfxId,
    Size2D size,
    SamplerProfile profile,
    TextureKind textureKind,
    TexturePixelFormat pixelFormat)
    : AssetObject(name, id, gid)
{
    public TextureId GfxId { get; } = gfxId;
    public SamplerProfile Profile { get; } = profile;
    public TextureKind TextureKind { get; } = textureKind;
    public TexturePixelFormat PixelFormat { get; } = pixelFormat;

    public Size2D Size { get; } = size;

    private TextureData? _textureData;

    //
    public override AssetCategory Category => AssetCategory.Graphic;
    public override AssetKind Kind => AssetKind.Texture;


    public bool HasPixelData => _textureData is not null;

    public bool TryGetPixelSpan(out ReadOnlySpan<byte> pixelData)
    {
        pixelData = Span<byte>.Empty;
        if (_textureData is not { } textureData) return false;
        pixelData = textureData.GetPixelData();
        return true;
    }

    internal void SetPixelData(TextureData textureData)
    {
        if (_textureData is not null) throw new InvalidOperationException("Texture already has a data entry.");
        _textureData = textureData;
    }
}