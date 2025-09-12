namespace ConcreteEngine.Graphics.Resources;

public readonly record struct GfxHandle(uint Slot, ushort Gen, ResourceKind Kind)
{
    public static GfxHandle MakeTexture2D(uint slot, ushort gen = 0) => new(slot, gen, ResourceKind.Texture);
    public static GfxHandle MakeShader(uint slot, ushort gen = 0) => new(slot, gen, ResourceKind.Shader);
    public static GfxHandle MakeVaoHandle(uint slot, ushort gen = 0) => new(slot, gen, ResourceKind.Mesh);
    public static GfxHandle MakeVboHandle(uint slot, ushort gen = 0) => new(slot, gen, ResourceKind.VertexBuffer);
    public static GfxHandle MakeIboHandle(uint slot, ushort gen = 0) => new(slot, gen, ResourceKind.IndexBuffer);
    public static GfxHandle MakeFboHandle(uint slot, ushort gen = 0) => new(slot, gen, ResourceKind.FrameBuffer);
    public static GfxHandle MakeRboHandle(uint slot, ushort gen = 0) => new(slot, gen, ResourceKind.RenderBuffer);
}