namespace ConcreteEngine.Graphics.Resources;

#region

using TextureOps = BackendOps<TextureId, GlTextureHandle, TextureMeta, TextureDef>;
using ShaderOps = BackendOps<ShaderId, GlShaderHandle, ShaderMeta, ShaderDef>;
using VertexArrayOps = BackendOps<MeshId, GlMeshHandle, MeshMeta, MeshDef>;
using VertexBufferOps = BackendOps<VertexBufferId, GlVboHandle, VertexBufferMeta, VertexBufferDef>;
using IndexBufferOps = BackendOps<IndexBufferId, GlIboHandle, IndexBufferMeta, IndexBufferDef>;
using FrameBufferOps = BackendOps<FrameBufferId, GlFboHandle, FrameBufferMeta, FrameBufferDef>;
using RenderBufferOps = BackendOps<RenderBufferId, GlRboHandle, RenderBufferMeta, RenderBufferDef>;
using UniformBufferOps = BackendOps<UniformBufferId, GlUboHandle, UniformBufferMeta, UniformBufferDef>;

#endregion

internal sealed class BackendOpsHub
{
    public TextureOps Texture { get; }
    public ShaderOps Shader { get; }
    public VertexArrayOps VertexArray { get; }
    public VertexBufferOps VertexBuffer { get; }
    public IndexBufferOps IndexBuffer { get; }
    public FrameBufferOps FrameBuffer { get; }
    public RenderBufferOps RenderBuffer { get; }
    public UniformBufferOps UniformBuffer { get; }


    // private readonly FrozenTypeRegistry<IResourceId, IBackendOps> _backendOps;

    internal BackendOpsHub(BackendStoreHub stores)
    {
        Texture = new TextureOps(stores);
        Shader = new ShaderOps(stores);
        VertexArray = new VertexArrayOps(stores);
        VertexBuffer = new VertexBufferOps(stores);
        IndexBuffer = new IndexBufferOps(stores);
        FrameBuffer = new FrameBufferOps(stores);
        RenderBuffer = new RenderBufferOps(stores);
        UniformBuffer = new UniformBufferOps(stores);
        /*
        _backendOps  = new FrozenTypeRegistry<IResourceId, IBackendOps>(8);
        _backendOps.Register<TextureId>(Texture);
        _backendOps.Register<ShaderId>(Shader);
        _backendOps.Register<MeshId>(VertexArray);
        _backendOps.Register<VertexBufferId>(VertexBuffer);
        _backendOps.Register<IndexBufferId>(IndexBuffer);
        _backendOps.Register<FrameBufferId>(FrameBuffer);
        _backendOps.Register<RenderBufferId>(RenderBuffer);
        _backendOps.Register<UniformBufferId>(UniformBuffer);
        _backendOps.Freeze();
        */
    }
}