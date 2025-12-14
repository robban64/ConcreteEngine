using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Definitions;

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
    public readonly Size2D Size = size;
    public readonly GfxFboColorTextureDesc? ColorTexture = colorTexture;
    public readonly GfxFboDepthTextureDesc? DepthTexture = depthTexture;
    public readonly bool ColorBuffer = colorBuffer;
    public readonly bool DepthStencilBuffer = depthStencilBuffer;
    public readonly RenderBufferMsaa Multisample = multisample;
}

public readonly struct GfxFboColorTextureDesc(
    TexturePixelFormat pixelFormat,
    TexturePreset texturePreset,
    GfxTextureBorder colorBorder
)
{
    public readonly TexturePixelFormat PixelFormat = pixelFormat;
    public readonly TexturePreset TexturePreset = texturePreset;
    public readonly GfxTextureBorder ColorBorder = colorBorder;

    public static GfxFboColorTextureDesc Off() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.None, GfxTextureBorder.Off);

    public static GfxFboColorTextureDesc Default() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.LinearClamp, GfxTextureBorder.Off);

    public static GfxFboColorTextureDesc DefaultMip() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.LinearMipmapClamp, GfxTextureBorder.Off);
}

public readonly struct GfxFboDepthTextureDesc(
    TexturePixelFormat pixelFormat,
    TexturePreset texturePreset,
    DepthMode compareTextureFunc,
    GfxTextureBorder borderColor
)
{
    public static GfxFboDepthTextureDesc Default() =>
        new(TexturePixelFormat.Depth, TexturePreset.LinearClampBorder, DepthMode.Lequal, GfxTextureBorder.On);

    public readonly TexturePixelFormat PixelFormat = pixelFormat;
    public readonly TexturePreset TexturePreset = texturePreset;
    public readonly DepthMode CompareTextureFunc = compareTextureFunc;
    public readonly GfxTextureBorder BorderColor = borderColor;
}