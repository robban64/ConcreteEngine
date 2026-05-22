using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly struct CreateTextureProps(
    float lod,
    TextureKind kind,
    TexturePixelFormat format,
    TexturePreset preset,
    TextureAnisotropy anisotropy,
    DepthMode compareTextureFunc = DepthMode.Unset,
    GpuTextureBorder borderColor = default,
    RenderBufferMsaa samples = RenderBufferMsaa.None
)
{
    public readonly GpuTextureBorder BorderColor = borderColor;
    
    public readonly Half Lod = (Half)lod;
    public readonly TextureKind Kind = kind;
    public readonly TexturePixelFormat Format = format;

    public readonly TexturePreset Preset = preset;
    public readonly TextureAnisotropy Anisotropy = anisotropy;
    public readonly DepthMode CompareTextureFunc = compareTextureFunc;
    public readonly RenderBufferMsaa Samples = samples;

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