namespace ConcreteEngine.Graphics.Gfx;

public struct FboAttachmentIds(
    TextureId colorTexture,
    TextureId depthTexture,
    RenderBufferId colorRbo,
    RenderBufferId depthRbo
)
{
    public TextureId ColorTexture = colorTexture;
    public TextureId DepthTexture = depthTexture;
    public RenderBufferId ColorRbo = colorRbo;
    public RenderBufferId DepthRbo = depthRbo;
}

public readonly struct FboColorAttachment(
    TexturePixelFormat pixelFormat,
    TexturePreset texturePreset,
    TextureBorder colorBorder
)
{
    public readonly TextureBorder ColorBorder = colorBorder;
    public readonly TexturePixelFormat PixelFormat = pixelFormat;
    public readonly TexturePreset TexturePreset = texturePreset;
    
    public bool IsEmpty() => PixelFormat == 0 && TexturePreset == 0;

    public static FboColorAttachment Off() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.None, TextureBorder.Off);

    public static FboColorAttachment Default() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.LinearClamp, TextureBorder.Off);

    public static FboColorAttachment DefaultMip() =>
        new(TexturePixelFormat.SrgbAlpha, TexturePreset.LinearMipmapClamp, TextureBorder.Off);
}

public readonly struct FboDepthAttachment(
    TexturePixelFormat pixelFormat,
    TexturePreset texturePreset,
    DepthMode compareTextureFunc,
    TextureBorder borderColor
)
{
    public static FboDepthAttachment Default() =>
        new(TexturePixelFormat.Depth, TexturePreset.LinearClampBorder, DepthMode.Lequal, TextureBorder.On);

    public readonly TextureBorder BorderColor = borderColor;
    public readonly TexturePixelFormat PixelFormat = pixelFormat;
    public readonly TexturePreset TexturePreset = texturePreset;
    public readonly DepthMode CompareTextureFunc = compareTextureFunc;
    
    public bool IsEmpty() => PixelFormat == 0 && TexturePreset == 0;

}