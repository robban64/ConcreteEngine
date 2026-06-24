using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

public struct TextureProperties(
    float lod,
    TextureKind kind = TextureKind.Texture2D,
    TexturePreset preset = TexturePreset.LinearClamp,
    AnisotropyLevel anisotropy = AnisotropyLevel.Off,
    TexturePixelFormat pixelFormat = TexturePixelFormat.SrgbAlpha)
{
    public float Lod = lod;
    public TextureKind Kind = kind;
    public TexturePreset Preset = preset;
    public AnisotropyLevel Anisotropy = anisotropy;
    public TexturePixelFormat PixelFormat = pixelFormat;
}

public sealed class Texture : AssetObject
{
    public readonly TextureId GfxId;

    public readonly GpuTextureState GpuState;

    public readonly Size2D Size;

    private TextureData? _textureData;

    //
    public override AssetCategory Category => AssetCategory.Graphic;
    public override AssetKind Kind => AssetKind.Texture;


    public Texture(string name, AssetId id, Guid gid, TextureId gfxId, Size2D size, TextureProperties props) :
        base(name, id, gid)
    {
        GfxId = gfxId;
        Size = size;
        GpuState = new GpuTextureState(this, props);
    }

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

    public TextureUsage Usage
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkDirty(AssetDirtyFlag.Metadata);
        }
    }
}