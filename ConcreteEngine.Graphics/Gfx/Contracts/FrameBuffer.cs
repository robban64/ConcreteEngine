#region

using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly record struct GfxFrameBufferDescriptor(
    Size2D Size,
    GfxFboColorTextureDesc? ColorTexture,
    GfxFboDepthTextureDesc? DepthTexture,
    bool ColorBuffer,
    bool DepthStencilBuffer,
    RenderBufferMsaa Multisample = RenderBufferMsaa.None
);

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