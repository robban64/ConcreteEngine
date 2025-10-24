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
        Texture = stores.GetStore<TextureId, GlTextureHandle>();
        Shader = stores.GetStore<ShaderId, GlShaderHandle>();
        VertexArray = stores.GetStore<MeshId, GlMeshHandle>();
        VertexBuffer = stores.GetStore<VertexBufferId, GlVboHandle>();
        IndexBuffer = stores.GetStore<IndexBufferId, GlIboHandle>();
        FrameBuffer = stores.GetStore<FrameBufferId, GlFboHandle>();
        RenderBuffer = stores.GetStore<RenderBufferId, GlRboHandle>();
        UniformBuffer = stores.GetStore<UniformBufferId, GlUboHandle>();
    }
}