using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
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


public sealed class Texture(string name, TextureId gfxId, Size2D size, TextureProperties properties) : AssetObject(name)
{
    // WIP
    public GfxAssetLink<TextureMeta> GfxLink { get; } = new(gfxId);
    
    public Size2D Size { get; } = size;

    private TextureProperties _properties = properties;

    //
    public override AssetCategory Category => AssetCategory.Graphic;
    public override AssetKind Kind => AssetKind.Texture;

    //TODO remove
    public ReadOnlyMemory<byte>? PixelData { get; private set; }
    public void SetPixelData(ReadOnlyMemory<byte> pixelData) => PixelData = pixelData;

    //
    public float LodBias
    {
        get => _properties.Lod;
        set
        {
            if (FloatMath.NearlyEqual(_properties.Lod, value)) return;
            _properties.Lod = value;
            MarkDirty();
        }
    }

    public TexturePixelFormat PixelFormat
    {
        get => _properties.PixelFormat;
        set
        {
            _properties.PixelFormat = value;
            MarkDirty();
        }
    }

    public TexturePreset Preset
    {
        get => _properties.Preset;
        set
        {
            _properties.Preset = value;
            MarkDirty();
        }
    }

    public TextureKind TextureKind
    {
        get => _properties.Kind;
        set
        {
            _properties.Kind = value;
            MarkDirty();
        }
    }


    public AnisotropyLevel Anisotropy
    {
        get => _properties.Anisotropy;
        set
        {
            _properties.Anisotropy = value;
            MarkDirty();
        }
    }

    public TextureUsage Usage
    {
        get;
        set
        {
            if(field == value) return;
            field = value;
            MarkDirty();
        }
    }
}