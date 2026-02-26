using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class Texture(string name) : AssetObject(name)
{
    public required TextureId GfxId { get; init; }

    public Size2D Size { get; init; }
    public int MipLevels { get; init; }
    
    public required TexturePixelFormat PixelFormat
    {
        get;
        set
        {
            field = value;
            MarkDirty();
        }
    } = TexturePixelFormat.SrgbAlpha;


    public float LodBias
    {
        get;
        set
        {
            if (FloatMath.NearlyEqual(field, value)) return;
            field = value;
            MarkDirty();
        }
    }

    public required TexturePreset Preset
    {
        get;
        set
        {
            field = value;
            MarkDirty();
        }
    } = TexturePreset.LinearClamp;

    public required TextureKind TextureKind
    {
        get;
        set
        {
            field = value;
            MarkDirty();
        }
    } = TextureKind.Texture2D;


    public required AnisotropyLevel Anisotropy
    {
        get;
        set
        {
            field = value;
            MarkDirty();
        }
    } = AnisotropyLevel.Off;

    public TextureUsage Usage
    {
        get;
        set
        {
            field = value;
            MarkDirty();
        }
    }

    public override AssetCategory Category => AssetCategory.Graphic;
    public override AssetKind Kind => AssetKind.Texture;

    //TODO remove
    public ReadOnlyMemory<byte>? PixelData { get; private set; }
    public void SetPixelData(ReadOnlyMemory<byte> pixelData) => PixelData = pixelData;

    internal override AssetObject CopyAndIncreaseGen() => throw new NotImplementedException();
}