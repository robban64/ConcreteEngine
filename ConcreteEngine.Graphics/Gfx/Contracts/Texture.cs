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
    float LodBias,
    TexturePreset Preset,
    TextureAnisotropy Anisotropy,
    DepthMode CompareTextureFunc = DepthMode.Unset,
    GfxTextureBorder BorderColor = default
);

public readonly record struct GfxTextureBorder(byte R, byte G, byte B, byte A, bool Enabled)
{
    public static GfxTextureBorder Off => new (0,0,0, 0,false);
    public static GfxTextureBorder On => new (1,1,1, 1,true);

}

internal readonly record struct GfxReplaceTexture(int Width, int Height, int? Depth = null, int? Samples = null);

internal readonly record struct BkTextureStoreDesc(GfxPixelFormat Format, uint Levels, uint Samples)
{
    public static BkTextureStoreDesc Make(GfxPixelFormat format, int levels, int samples) =>
        new(format, (uint)levels, (uint)samples);
}

internal readonly record struct BkTextureUploadDesc(GfxPixelFormat Format, uint Levels, uint Samples);