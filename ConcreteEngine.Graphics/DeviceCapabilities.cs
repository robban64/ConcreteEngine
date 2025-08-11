namespace ConcreteEngine.Graphics;

public sealed record DeviceCapabilities
{
    public int MaxTextureSize { get; init; }
    public int MaxTextureImageUnits { get; init; }
    public int MaxArrayTextureLayers { get; init; }
    public int MaxSamples { get; init; }
    public int MaxFramebufferWidth { get; init; }
    public int MaxFramebufferHeight { get; init; }
    public int MaxColorAttachments { get; init; }
    public int MaxUniformBlockSize { get; init; }
    public int MaxVertexAttribBindings { get; init; }
}