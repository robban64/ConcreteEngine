namespace ConcreteEngine.Graphics.Configuration;

public static class GfxLimits
{
    public const int MaxTextureSize = 2048;
    public const int MaxDepthTextureSize = 8192;
    public const int TextureSlots = 16;

    public const int MaxVboBindings = 4;
    public const int MaxVertexAttribs = 16;

    public const int MaxPlainUniforms = 8;

    public const int StoreLimit = 10_000;


    public const int LargeCapacity = 256;
    public const int MediumCapacity = 64;
    public const int LowCapacity = 32;
}