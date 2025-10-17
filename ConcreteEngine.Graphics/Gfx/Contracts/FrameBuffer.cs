#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly struct GfxFrameBufferDescriptor(
    Size2D size,
    GfxFboColorTextureDesc? colorTexture,
    GfxFboDepthTextureDesc? depthTexture,
    bool colorBuffer,
    bool depthStencilBuffer,
    RenderBufferMsaa multisample = RenderBufferMsaa.None
)
{
    public Size2D Size { get; init; } = size;
    public GfxFboColorTextureDesc? ColorTexture { get; init; } = colorTexture;
    public GfxFboDepthTextureDesc? DepthTexture { get; init; } = depthTexture;
    public bool ColorBuffer { get; init; } = colorBuffer;
    public bool DepthStencilBuffer { get; init; } = depthStencilBuffer;
    public RenderBufferMsaa Multisample { get; init; } = multisample;

}

public readonly record struct GfxFboColorTextureDesc(
    TexturePixelFormat PixelFormat,
    TexturePreset TexturePreset,
    GfxTextureBorder ColorBorder
)
{
    public static GfxFboColorTextureDesc Off() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.None, GfxTextureBorder.Off);

    public static GfxFboColorTextureDesc Default() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.LinearClamp, GfxTextureBorder.Off);

    public static GfxFboColorTextureDesc DefaultMip() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.LinearMipmapClamp, GfxTextureBorder.Off);
}

public readonly record struct GfxFboDepthTextureDesc(
    TexturePixelFormat PixelFormat,
    TexturePreset TexturePreset,
    DepthMode CompareTextureFunc,
    GfxTextureBorder BorderColor
)
{
    public static GfxFboDepthTextureDesc Default() =>
        new(TexturePixelFormat.Depth, TexturePreset.LinearClampBorder, DepthMode.Lequal, GfxTextureBorder.On);
}