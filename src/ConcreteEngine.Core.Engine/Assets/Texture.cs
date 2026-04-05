using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public struct TextureProperties(
    float lodBias,
    int mipLevels,
    TextureKind kind = TextureKind.Texture2D,
    TextureUsage Usage = TextureUsage.Albedo,
    TexturePreset preset = TexturePreset.LinearClamp,
    AnisotropyLevel anisotropy = AnisotropyLevel.Off,
    TexturePixelFormat pixelFormat = TexturePixelFormat.SrgbAlpha)
{
    public float LodBias = lodBias;
    public int MipLevels = mipLevels;
    public  TextureKind Kind = kind;
    public TextureUsage Usage = Usage;
    public  TexturePreset Preset = preset;
    public  AnisotropyLevel Anisotropy = anisotropy;
    public  TexturePixelFormat PixelFormat = pixelFormat;
}

public sealed class Texture(string name, TextureId gfxId, Size2D size, TextureProperties properties) : AssetObject(name)
{
    public TextureId GfxId { get; } = gfxId;
    public Size2D Size { get; } = size;
    
    private TextureProperties _properties = properties;
    
    //
    public override AssetCategory Category => AssetCategory.Graphic;
    public override AssetKind Kind => AssetKind.Texture;

    //TODO remove
    public ReadOnlyMemory<byte>? PixelData { get; private set; }
    public void SetPixelData(ReadOnlyMemory<byte> pixelData) => PixelData = pixelData;

    //
    public int MipLevels => _properties.MipLevels;

    public float LodBias
    {
        get => _properties.LodBias;
        set
        {
            if (FloatMath.NearlyEqual(_properties.LodBias, value)) return;
            _properties.LodBias = value;
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

    public  TexturePreset Preset
    {
        get => _properties.Preset;
        set
        {
            _properties.Preset = value;
            MarkDirty();
        }
    }

    public  TextureKind TextureKind
    {
        get => _properties.Kind;
        set
        {
            _properties.Kind = value;
            MarkDirty();
        }
    } 


    public  AnisotropyLevel Anisotropy
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
        get => _properties.Usage;
        set
        {
            _properties.Usage = value;
            MarkDirty();
        }
    }


    internal override AssetObject CopyAndIncreaseGen() => throw new NotImplementedException();
}