namespace ConcreteEngine.Graphics.Resources;

public enum ResourceKind : byte
{
    Invalid = 0,
    Texture = 1,
    Shader = 2,
    Mesh = 3,
    VertexBuffer = 4,
    IndexBuffer = 5,
    FrameBuffer = 6,
    RenderBuffer = 7,
    UniformBuffer = 8,
}