namespace ConcreteEngine.Renderer.Data;

public static class RenderLimits
{
    public const int BoneCapacity = 64;

    public const int MinOutputSize = 16;
    public const int MaxOutputSize = 8192;

    public const int MinShadowMapSize = 512;
    public const int MaxShadowMapSize = 8192;

    public const int TextureSlots = 16;
    public const int PassSlots = 16;
    public const int UboSlots = 16;
    public const int FboSlots = 16;

    public const int MaxFboVariants = 4;

    public const int DefaultMaterialBufferCapacity = 512;

    public const int MaxMaterialCount = 1024;
    public const int MaxMaterialBufferCapacity = 2048;

    public const int MaxCommandBuffCapacity = 20_480;
    public const int MaxTextureSlotBuffCapacity = 20_480;


    public const int MaxSpriteBatchSize = 1024;
}