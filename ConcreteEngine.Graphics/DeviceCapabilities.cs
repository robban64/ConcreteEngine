namespace ConcreteEngine.Graphics;

public readonly record struct OpenGlVersion(int Major, int Minor)
{
    public int Version => Major * 100 + Minor * 10;
}

public sealed record DeviceCapabilities
{
    public OpenGlVersion GlVersion { get; init; }
    public int MaxTextureSize { get; init; }
    public int MaxTextureImageUnits { get; init; }
    public int MaxArrayTextureLayers { get; init; }
    public int MaxSamples { get; init; }
    public int MaxFramebufferWidth { get; init; }
    public int MaxFramebufferHeight { get; init; }
    public int MaxColorAttachments { get; init; }
    public int MaxVertexAttribBindings { get; init; }
    public float MaxAnisotropy  { get; init; }
    public int MaxUniformBlockSize { get; init; }
    public int UniformBufferOffsetAlignment { get; init; }

}