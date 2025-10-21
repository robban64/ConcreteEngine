namespace ConcreteEngine.Graphics.Gfx.Definitions;

public enum ResourceKind : byte
{
    Invalid = 0,
    Texture = 1,
    Shader = 2,
    Mesh = 3,
    VertexBuffer = 4,
    IndexBuffer = 5,
    UniformBuffer = 6,
    FrameBuffer = 7,
    RenderBuffer = 8
}

public static class ResourceKindNames
{
    public const string Invalid = "Invalid";
    public const string Texture = "Texture";
    public const string Shader = "Shader";
    public const string Mesh = "Mesh";
    public const string Vbo = "VBO";
    public const string Ibo = "IBO";
    public const string Ubo = "UBO";
    public const string FrameBuffer = "FBO";
    public const string RenderBuffer = "RBO";

    public static string ToSimpleName(this ResourceKind kind)
    {
        return kind switch
        {
            ResourceKind.Texture => Texture,
            ResourceKind.Shader => Shader,
            ResourceKind.Mesh => Mesh,
            ResourceKind.VertexBuffer => Vbo,
            ResourceKind.IndexBuffer => Ibo,
            ResourceKind.UniformBuffer => Ubo,
            ResourceKind.FrameBuffer => FrameBuffer,
            ResourceKind.RenderBuffer => RenderBuffer,
            _ => Invalid
        };
    }
}