#region

using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly struct GfxTextureBorder(byte r, byte g, byte b, byte a, bool enabled)
{
    public readonly byte R = r;
    public readonly byte G = g;
    public readonly byte B = b;
    public readonly byte A = a;
    public readonly bool Enabled = enabled;

    public static GfxTextureBorder Off => new(0, 0, 0, 0, false);
    public static GfxTextureBorder On => new(1, 1, 1, 1, true);
}

public readonly struct GfxTextureDescriptor(
    int width,
    int height,
    TextureKind kind,
    TexturePixelFormat format,
    int depth = 1,
    RenderBufferMsaa samples = RenderBufferMsaa.None
)
{
    public readonly int Width = width, Height = height, Depth = depth;
    public TextureKind Kind { get; init; } = kind;
    public TexturePixelFormat Format { get; init; } = format;
    public RenderBufferMsaa Samples { get; init; } = samples;
}

public readonly struct GfxTextureProperties(
    float lodBias,
    TexturePreset preset,
    TextureAnisotropy anisotropy,
    DepthMode compareTextureFunc = DepthMode.Unset,
    GfxTextureBorder borderColor = default
)
{
    public float LodBias { get; init; } = lodBias;
    public TexturePreset Preset { get; init; } = preset;
    public TextureAnisotropy Anisotropy { get; init; } = anisotropy;
    public DepthMode CompareTextureFunc { get; init; } = compareTextureFunc;
    public GfxTextureBorder BorderColor { get; init; } = borderColor;
}

internal readonly struct GfxReplaceTexture(int width, int height, int? depth = null, int? samples = null)
{
    public readonly int Width = width, Height = height;
    public readonly int? Depth = depth, Samples = samples;
}

internal readonly struct BkTextureStoreDesc(TexturePixelFormat format, uint levels, uint samples)
{
    public readonly uint Levels = levels, Samples = samples;
    public readonly TexturePixelFormat Format = format;

    public static BkTextureStoreDesc Make(TexturePixelFormat format, int levels, int samples) =>
        new(format, (uint)levels, (uint)samples);
}