#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.Contracts;

public readonly record struct GfxTextureDescriptor(
    int Width,
    int Height,
    TextureKind Kind,
    EnginePixelFormat Format,
    int Depth,
    RenderBufferMsaa Msaa
)
{
    /*
    public static GfxTextureDescriptor MakeFboMsaaDesc(int width, int height) =>
        new(width, height, TexturePreset.None, TextureKind.Multisample2D, EnginePixelFormat.SrgbAlpha,
            TextureAnisotropy.Off, 0);

    public static GfxTextureDescriptor MakeFboColorDesc(int width, int height, TexturePreset preset) =>
        new(width, height, preset, TextureKind.Texture2D, EnginePixelFormat.SrgbAlpha,
            TextureAnisotropy.Off, 0);
            */
}

public readonly record struct GfxTextureProperties(
    TexturePreset Preset,
    TextureAnisotropy Anisotropy,
    float LodBias
);

internal readonly record struct GfxReplaceTexture(int Width, int Height, int Depth = 1, int? Samples = null);


internal readonly record struct BkTextureStoreDesc(EnginePixelFormat Format, uint Levels, uint Samples)
{
    public static BkTextureStoreDesc Make(EnginePixelFormat format, int levels) 
        => new(format, (uint)levels, 0);

    public static BkTextureStoreDesc MakeMultiSample(EnginePixelFormat format, RenderBufferMsaa msaa) =>
        new(format, 0, (uint)msaa.ToSamples());
}

internal readonly record struct BkTextureUploadDesc(EnginePixelFormat Format, uint Levels, uint Samples);