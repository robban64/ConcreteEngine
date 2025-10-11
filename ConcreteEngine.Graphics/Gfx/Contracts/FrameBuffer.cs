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
)
{
    public GfxPixelFormat PixelFormat =>
        ColorTexture?.PixelFormat ?? DepthTexture?.PixelFormat ?? GfxPixelFormat.Unknown;

    public TexturePreset TexturePreset =>
        ColorTexture?.TexturePreset ?? DepthTexture?.TexturePreset ?? TexturePreset.None;

    public DepthMode CompareTextureFunc => DepthTexture?.CompareTextureFunc ?? DepthMode.Unset;
}

public readonly record struct GfxFboColorTextureDesc(
    GfxPixelFormat PixelFormat = GfxPixelFormat.SrgbAlpha,
    TexturePreset TexturePreset = TexturePreset.LinearClamp
)
{
    public GfxTextureBorder BorderColor { get; init; } = GfxTextureBorder.Off;
}

public readonly record struct GfxFboDepthTextureDesc(
    GfxPixelFormat PixelFormat = GfxPixelFormat.Depth,
    TexturePreset TexturePreset = TexturePreset.LinearClampBorder,
    DepthMode CompareTextureFunc = DepthMode.Lequal
)
{
    public GfxTextureBorder BorderColor { get; init; } = GfxTextureBorder.On;
}