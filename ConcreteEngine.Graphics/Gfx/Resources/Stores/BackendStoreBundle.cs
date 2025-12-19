using ConcreteEngine.Graphics.Gfx.Resources.Handles;

namespace ConcreteEngine.Graphics.Gfx.Resources.Stores;

internal sealed class BackendStoreBundle
{
    public readonly BackendResourceStore<TextureId, GlTextureHandle> Texture;
    public readonly BackendResourceStore<ShaderId, GlShaderHandle> Shader;
    public readonly BackendResourceStore<MeshId, GlMeshHandle> VertexArray;
    public readonly BackendResourceStore<VertexBufferId, GlVboHandle> VertexBuffer;
    public readonly BackendResourceStore<IndexBufferId, GlIboHandle> IndexBuffer;
    public readonly BackendResourceStore<FrameBufferId, GlFboHandle> FrameBuffer;
    public readonly BackendResourceStore<RenderBufferId, GlRboHandle> RenderBuffer;
    public readonly BackendResourceStore<UniformBufferId, GlUboHandle> UniformBuffer;


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