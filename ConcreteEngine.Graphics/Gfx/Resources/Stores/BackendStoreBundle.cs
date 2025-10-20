namespace ConcreteEngine.Graphics.Gfx.Resources;

internal sealed class BackendStoreBundle
{
    public BackendResourceStore<TextureId, GlTextureHandle> Texture { get; }
    public BackendResourceStore<ShaderId, GlShaderHandle> Shader { get; }
    public BackendResourceStore<MeshId, GlMeshHandle> VertexArray { get; }
    public BackendResourceStore<VertexBufferId, GlVboHandle> VertexBuffer { get; }
    public BackendResourceStore<IndexBufferId, GlIboHandle> IndexBuffer { get; }
    public BackendResourceStore<FrameBufferId, GlFboHandle> FrameBuffer { get; }
    public BackendResourceStore<RenderBufferId, GlRboHandle> RenderBuffer { get; }
    public BackendResourceStore<UniformBufferId, GlUboHandle> UniformBuffer { get; }


    internal BackendStoreBundle(BackendStoreHub stores)
    {
        Texture = stores.Get<TextureId, GlTextureHandle>();
        Shader = stores.Get<ShaderId, GlShaderHandle>();
        VertexArray = stores.Get<MeshId, GlMeshHandle>();
        VertexBuffer = stores.Get<VertexBufferId, GlVboHandle>();
        IndexBuffer = stores.Get<IndexBufferId, GlIboHandle>();
        FrameBuffer = stores.Get<FrameBufferId, GlFboHandle>();
        RenderBuffer = stores.Get<RenderBufferId, GlRboHandle>();
        UniformBuffer = stores.Get<UniformBufferId, GlUboHandle>();
    }
}