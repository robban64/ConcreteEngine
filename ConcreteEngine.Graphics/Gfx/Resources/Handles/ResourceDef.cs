using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal interface IResourceRefToken<out TId, out THandle, TMeta>
    where TId : unmanaged, IResourceId
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    where TMeta : unmanaged, IResourceMeta
{
    static abstract ResourceKind Kind { get; }
    static abstract TId MakeId(int raw);
    static abstract THandle MakeHandle(uint raw);
}

internal readonly struct TextureDef
    : IResourceRefToken<TextureId, GlTextureHandle, TextureMeta>
{
    public static ResourceKind Kind => ResourceKind.Texture;
    public static TextureId MakeId(int raw) => new(raw + 1);
    public static GlTextureHandle MakeHandle(uint h) => new(h);
}

internal readonly struct ShaderDef
    : IResourceRefToken<ShaderId, GlShaderHandle, ShaderMeta>
{
    public static ResourceKind Kind => ResourceKind.Shader;
    public static ShaderId MakeId(int raw) => new(raw + 1);
    public static GlShaderHandle MakeHandle(uint h) => new(h);
}

internal readonly struct MeshDef
    : IResourceRefToken<MeshId, GlMeshHandle, MeshMeta>
{
    public static ResourceKind Kind => ResourceKind.Mesh;
    public static MeshId MakeId(int raw) => new(raw + 1);
    public static GlMeshHandle MakeHandle(uint h) => new(h);
}

internal readonly struct VertexBufferDef
    : IResourceRefToken<VertexBufferId, GlVboHandle, VertexBufferMeta>
{
    public static ResourceKind Kind => ResourceKind.VertexBuffer;
    public static VertexBufferId MakeId(int raw) => new(raw + 1);
    public static GlVboHandle MakeHandle(uint h) => new(h);
}

internal readonly struct IndexBufferDef
    : IResourceRefToken<IndexBufferId, GlIboHandle, IndexBufferMeta>
{
    public static ResourceKind Kind => ResourceKind.IndexBuffer;
    public static IndexBufferId MakeId(int raw) => new(raw + 1);
    public static GlIboHandle MakeHandle(uint h) => new(h);
}

internal readonly struct FrameBufferDef
    : IResourceRefToken<FrameBufferId, GlFboHandle, FrameBufferMeta>
{
    public static ResourceKind Kind => ResourceKind.FrameBuffer;
    public static FrameBufferId MakeId(int raw) => new(raw + 1);
    public static GlFboHandle MakeHandle(uint h) => new(h);
}

internal readonly struct RenderBufferDef
    : IResourceRefToken<RenderBufferId, GlRboHandle, RenderBufferMeta>
{
    public static ResourceKind Kind => ResourceKind.RenderBuffer;
    public static RenderBufferId MakeId(int raw) => new(raw + 1);
    public static GlRboHandle MakeHandle(uint h) => new(h);
}

internal readonly struct UniformBufferDef
    : IResourceRefToken<UniformBufferId, GlUboHandle, UniformBufferMeta>
{
    public static ResourceKind Kind => ResourceKind.UniformBuffer;
    public static UniformBufferId MakeId(int raw) => new(raw + 1);
    public static GlUboHandle MakeHandle(uint h) => new(h);
}