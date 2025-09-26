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
    int Depth = 1,
    RenderBufferMsaa Samples = RenderBufferMsaa.None
);

public readonly record struct GfxTextureProperties(
    TexturePreset Preset,
    TextureAnisotropy Anisotropy,
    float LodBias
);

internal readonly record struct GfxReplaceTexture(int Width, int Height, int? Depth = null, int? Samples = null);


internal readonly record struct BkTextureStoreDesc(EnginePixelFormat Format, uint Levels, uint Samples)
{
    public static BkTextureStoreDesc Make(EnginePixelFormat format, int levels, int samples) 
        => new(format, (uint)levels, (uint) samples);

}

internal readonly record struct BkTextureUploadDesc(EnginePixelFormat Format, uint Levels, uint Samples);