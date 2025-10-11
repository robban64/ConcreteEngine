namespace ConcreteEngine.Graphics;

public sealed record GraphicsConfiguration
{
    public int MaxTextureSize { get; private set; } = 2048;
    public int MaxDepthTextureSize { get; private set; } = 8192;
    public int TextureSlots { get; private set; } = 16;
}