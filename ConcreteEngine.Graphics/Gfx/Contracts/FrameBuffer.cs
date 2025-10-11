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
    GfxPixelFormat PixelFormat,
    TexturePreset TexturePreset,
    GfxTextureBorder ColorBorder
)
{
    public static GfxFboColorTextureDesc Off() =>
        new(GfxPixelFormat.SrgbAlpha, TexturePreset.None, GfxTextureBorder.Off);

    public static GfxFboColorTextureDesc Default() =>
        new(GfxPixelFormat.SrgbAlpha, TexturePreset.LinearClamp, GfxTextureBorder.Off);

    public static GfxFboColorTextureDesc DefaultMip() =>
        new(GfxPixelFormat.SrgbAlpha, TexturePreset.LinearMipmapClamp, GfxTextureBorder.Off);
}

public readonly record struct GfxFboDepthTextureDesc(
    GfxPixelFormat PixelFormat,
    TexturePreset TexturePreset,
    DepthMode CompareTextureFunc,
    GfxTextureBorder BorderColor
)
{
    public static GfxFboDepthTextureDesc Default() =>
        new(GfxPixelFormat.Depth, TexturePreset.LinearClampBorder, DepthMode.Lequal, GfxTextureBorder.On);
}