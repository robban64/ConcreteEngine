namespace ConcreteEngine.Graphics.Resources;


public readonly record struct GfxHandle(uint Slot, ushort Gen, ResourceKind Kind)
{
    public bool IsValid =>  Gen > 0 && Kind != ResourceKind.Invalid;
}



internal interface IResourceDef<out TId, out THandle, TMeta>
    where TId : unmanaged, IResourceId
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    where TMeta : unmanaged, IResourceMeta
{
    static abstract ResourceKind Kind { get; }
    static abstract TId MakeId(int raw);
    static abstract THandle MakeHandle(uint raw);
}

internal readonly struct ResourceRef<TId> : IEquatable<ResourceRef<TId>> where TId : unmanaged, IResourceId
{
    public readonly GfxHandle Handle;
    public ResourceRef(in GfxHandle handle) => Handle = handle;
    public bool Equals(ResourceRef<TId> other) =>  Handle == other.Handle;
}

internal readonly struct TextureDef
    : IResourceDef<TextureId, GlTextureHandle, TextureMeta>
{
    public static ResourceKind Kind => ResourceKind.Texture;
    public static TextureId        MakeId(int raw)    => new(raw + 1);
    public static GlTextureHandle  MakeHandle(uint h) => new(h);
}

internal readonly struct ShaderDef
    : IResourceDef<ShaderId, GlShaderHandle, ShaderMeta>
{
    public static ResourceKind Kind => ResourceKind.Shader;
    public static ShaderId       MakeId(int raw)    => new(raw + 1);
    public static GlShaderHandle MakeHandle(uint h) => new(h);
}

internal readonly struct MeshDef
    : IResourceDef<MeshId, GlMeshHandle, MeshMeta>
{
    public static ResourceKind Kind => ResourceKind.Mesh;
    public static MeshId       MakeId(int raw)    => new(raw + 1);
    public static GlMeshHandle MakeHandle(uint h) => new(h);
}

internal readonly struct VertexBufferDef
    : IResourceDef<VertexBufferId, GlVboHandle, VertexBufferMeta>
{
    public static ResourceKind Kind => ResourceKind.VertexBuffer;
    public static VertexBufferId MakeId(int raw)   => new(raw + 1);
    public static GlVboHandle    MakeHandle(uint h)=> new(h);
}

internal readonly struct IndexBufferDef
    : IResourceDef<IndexBufferId, GlIboHandle, IndexBufferMeta>
{
    public static ResourceKind Kind => ResourceKind.IndexBuffer;
    public static IndexBufferId MakeId(int raw)   => new(raw + 1);
    public static GlIboHandle   MakeHandle(uint h)=> new(h);
}

internal readonly struct FrameBufferDef
    : IResourceDef<FrameBufferId, GlFboHandle, FrameBufferMeta>
{
    public static ResourceKind Kind => ResourceKind.FrameBuffer;
    public static FrameBufferId MakeId(int raw)   => new(raw + 1);
    public static GlFboHandle   MakeHandle(uint h)=> new(h);
}

internal readonly struct RenderBufferDef
    : IResourceDef<RenderBufferId, GlRboHandle, RenderBufferMeta>
{
    public static ResourceKind Kind => ResourceKind.RenderBuffer;
    public static RenderBufferId MakeId(int raw)   => new(raw + 1);
    public static GlRboHandle    MakeHandle(uint h)=> new(h);
}

internal readonly struct UniformBufferDef
    : IResourceDef<UniformBufferId, GlUboHandle, UniformBufferMeta>
{
    public static ResourceKind Kind => ResourceKind.UniformBuffer;
    public static UniformBufferId MakeId(int raw)    => new(raw + 1);
    public static GlUboHandle     MakeHandle(uint h) => new(h);
}


/*
static THandle GetHandle<TId, THandle, TMeta, TType>(
    BackendStores stores, in GfxHandle h)
    where TId     : unmanaged, IResourceId
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    where TMeta   : unmanaged, IResourceMeta
    where TType   : IResourceType<TId, THandle, TMeta>
{
    return stores.Get<TId, THandle, TMeta, TType>().Get(in h);
}
*/