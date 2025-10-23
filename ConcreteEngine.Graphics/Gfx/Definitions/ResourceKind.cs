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