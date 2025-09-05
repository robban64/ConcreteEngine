#region

#endregion

namespace ConcreteEngine.Graphics.Descriptors;

public readonly record struct TextureDesc(
    byte[] PixelData,
    int Width,
    int Height,
    EnginePixelFormat Format,
    TexturePreset Preset,
    int? Anisotropy = null,
    float LodBias = 0,
    bool NullPtrData = false
);

public readonly struct CreateCubemapDesc(
    Func<CubemapFaceDesc>[] loaders,
    int width,
    int height,
    EnginePixelFormat format
)
{
    public readonly Func<CubemapFaceDesc>[] Loaders = loaders;
    public readonly int Width  = width;
    public readonly int Height = height;
    public readonly EnginePixelFormat Format  = format;
}

public readonly record struct CubemapFaceDesc(
    byte[] PixelData,
    int Width,
    int Height,
    EnginePixelFormat Format
);