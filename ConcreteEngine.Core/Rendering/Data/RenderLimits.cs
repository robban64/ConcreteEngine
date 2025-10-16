namespace ConcreteEngine.Core.Rendering.Data;

public static class RenderLimits
{
    public const int TextureSlots = 16;
    public const int PassSlots = 16;
    public const int UboSlots = 16;
    public const int FboSlots = 16;

    public const int MaxFboVariants = 4;

    public const int DefaultCommandBuffCapacity = 128;
    public const int MaxCommandBuffCapacity = 10_000;
    public const int MaxCmdBuffTransformBufferCapacity = 32;
    
    public const int DefaultMaterialBufferCapacity = 16;
    public const int MaxMaterialCount = 1024;
    public const int MaxMaterialBufferCapacity = 1024;


    public const int MinShadowMapSize = 512;
    public const int MaxShadowMapSize = 8192;

    public const int MaxSpriteBatchSize = 1024;
}