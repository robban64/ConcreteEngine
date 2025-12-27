namespace ConcreteEngine.Graphics.Configuration;

public readonly record struct OpenGlVersion(int Major, int Minor)
{
    public int Version => Major * 100 + Minor * 10;
}

public sealed record DeviceCapabilities
{
    public OpenGlVersion GlVersion;
    public int MaxTextureSize;
    public int MaxTextureImageUnits;
    public int MaxArrayTextureLayers;
    public int MaxSamples;
    public int MaxFramebufferWidth;
    public int MaxFramebufferHeight;
    public int MaxColorAttachments;
    public int MaxVertexAttribBindings;
    public float MaxAnisotropy;
    public int MaxUniformBlockSize;
    public int UniformBufferOffsetAlignment;
}