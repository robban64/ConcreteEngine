namespace ConcreteEngine.Graphics;

public sealed record GraphicsConfiguration
{
    public int MaxTextureSize { get; set; } = 2048;
    public int MaxTextureImageUnits { get; set; } = 16;
}