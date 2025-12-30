using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly struct CreateTextureInfo(
    int width,
    int height,
    TextureKind kind,
    TexturePixelFormat format,
    int depth = 1,
    RenderBufferMsaa samples = RenderBufferMsaa.None
)
{
    public readonly int Width = width, Height = height, Depth = depth;
    public readonly TextureKind Kind = kind;
    public readonly TexturePixelFormat Format = format;
    public readonly RenderBufferMsaa Samples = samples;
}

public readonly struct CreateTextureProps(
    float lodBias,
    TexturePreset preset,
    TextureAnisotropy anisotropy,
    DepthMode compareTextureFunc = DepthMode.Unset,
    GpuTextureBorder borderColor = default
)
{
    public readonly float LodBias  = lodBias;
    public readonly TexturePreset Preset  = preset;
    public readonly TextureAnisotropy Anisotropy  = anisotropy;
    public readonly DepthMode CompareTextureFunc  = compareTextureFunc;
    public readonly GpuTextureBorder BorderColor  = borderColor;
}

internal readonly struct ReplaceTextureProps(int width, int height, int? depth = null, int? samples = null)
{
    public readonly int Width = width, Height = height;
    public readonly int? Depth = depth, Samples = samples;
}

internal readonly struct GpuTextureProps(TexturePixelFormat format, uint levels, uint samples)
{
    public readonly uint Levels = levels, Samples = samples;
    public readonly TexturePixelFormat Format = format;

    public static GpuTextureProps Make(TexturePixelFormat format, int levels, int samples) =>
        new(format, (uint)levels, (uint)samples);
}