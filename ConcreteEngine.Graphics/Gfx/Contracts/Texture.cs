#region

using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly record struct GfxTextureDescriptor(
    int Width,
    int Height,
    TextureKind Kind,
    GfxPixelFormat Format,
    int Depth = 1,
    RenderBufferMsaa Samples = RenderBufferMsaa.None
);

public readonly record struct GfxTextureProperties(
    TexturePreset Preset,
    TextureAnisotropy Anisotropy,
    float LodBias
);

internal readonly record struct GfxReplaceTexture(int Width, int Height, int? Depth = null, int? Samples = null);

internal readonly record struct BkTextureStoreDesc(GfxPixelFormat Format, uint Levels, uint Samples)
{
    public static BkTextureStoreDesc Make(GfxPixelFormat format, int levels, int samples) =>
        new(format, (uint)levels, (uint)samples);
}

internal readonly record struct BkTextureUploadDesc(GfxPixelFormat Format, uint Levels, uint Samples);