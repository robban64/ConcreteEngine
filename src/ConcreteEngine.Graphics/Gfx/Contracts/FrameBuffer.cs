using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public readonly struct CreateFboInfo(
    Size2D size,
    FboColorAttachment? colorTexture,
    FboDepthAttachment? depthTexture,
    bool colorBuffer,
    bool depthStencilBuffer,
    RenderBufferMsaa multisample = RenderBufferMsaa.None
)
{
    public readonly Size2D Size = size;
    public readonly FboColorAttachment? ColorTexture = colorTexture;
    public readonly FboDepthAttachment? DepthTexture = depthTexture;
    public readonly bool ColorBuffer = colorBuffer;
    public readonly bool DepthStencilBuffer = depthStencilBuffer;
    public readonly RenderBufferMsaa Multisample = multisample;
}

public readonly struct FboColorAttachment(
    TexturePixelFormat pixelFormat,
    TexturePreset texturePreset,
    GpuTextureBorder colorBorder
)
{
    public readonly GpuTextureBorder ColorBorder = colorBorder;
    public readonly TexturePixelFormat PixelFormat = pixelFormat;
    public readonly TexturePreset TexturePreset = texturePreset;

    public static FboColorAttachment Off() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.None, GpuTextureBorder.Off);

    public static FboColorAttachment Default() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.LinearClamp, GpuTextureBorder.Off);

    public static FboColorAttachment DefaultMip() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.LinearMipmapClamp, GpuTextureBorder.Off);
}

public readonly struct FboDepthAttachment(
    TexturePixelFormat pixelFormat,
    TexturePreset texturePreset,
    DepthMode compareTextureFunc,
    GpuTextureBorder borderColor
)
{
    public static FboDepthAttachment Default() =>
        new(TexturePixelFormat.Depth, TexturePreset.LinearClampBorder, DepthMode.Lequal, GpuTextureBorder.On);

    public readonly GpuTextureBorder BorderColor = borderColor;
    public readonly TexturePixelFormat PixelFormat = pixelFormat;
    public readonly TexturePreset TexturePreset = texturePreset;
    public readonly DepthMode CompareTextureFunc = compareTextureFunc;
}