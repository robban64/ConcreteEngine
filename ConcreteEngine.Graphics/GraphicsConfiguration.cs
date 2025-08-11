namespace ConcreteEngine.Graphics;

public sealed record GraphicsConfiguration(DeviceCapabilities Capabilities)
{
    public int MaxTextureSize { get; set; } = 2048;
    public int MaxTextureImageUnits { get; set; } = 6;
    public int MaxRenderPasses { get; set; } = 16;

    public int MinSpriteBatchSize { get; set; } = 64;
    public int MaxSpriteBatchSize { get; set; } = 1024;
    
    public int MaxSpriteBatchInstanceCount { get; set; } = 16;
}