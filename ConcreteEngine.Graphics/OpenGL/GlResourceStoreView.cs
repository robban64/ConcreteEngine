#region

using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

interface IResourceStoreView<THandle> where THandle : unmanaged
{
    public ResourceStore<TextureId, TextureMeta, GlTextureHandle> TextureStore { get; }
    public ResourceStore<ShaderId, ShaderMeta, GlShaderHandle> ShaderStore { get; }
    public ResourceStore<MeshId, MeshMeta, GlMeshHandle> MeshStore { get; }
    public ResourceStore<VertexBufferId, VertexBufferMeta, GlVertexBufferHandle> VboStore { get; }
    public ResourceStore<IndexBufferId, IndexBufferMeta, GlIndexBufferHandle> IboStore { get; }
    public ResourceStore<FrameBufferId, FrameBufferMeta, GlFrameBufferHandle> FboStore { get; }
    public ResourceStore<RenderBufferId, RenderBufferMeta, GlRenderBufferHandle> RboStore { get; }
    public ResourceStore<UniformBufferId, UniformBufferMeta, GlUniformBufferHandle>  UboStore { get; }
}

internal sealed class GlResourceStoreView
{
    public MeshRegistry MeshRegistry { get; }
    public ShaderRegistry ShaderRegistry { get; }
    public ResourceStore<TextureId, TextureMeta, GlTextureHandle> TextureStore { get; }
    public ResourceStore<ShaderId, ShaderMeta, GlShaderHandle> ShaderStore { get; }
    public ResourceStore<MeshId, MeshMeta, GlMeshHandle> MeshStore { get; }
    public ResourceStore<VertexBufferId, VertexBufferMeta, GlVertexBufferHandle> VboStore { get; }
    public ResourceStore<IndexBufferId, IndexBufferMeta, GlIndexBufferHandle> IboStore { get; }
    public ResourceStore<FrameBufferId, FrameBufferMeta, GlFrameBufferHandle> FboStore { get; }
    public ResourceStore<RenderBufferId, RenderBufferMeta, GlRenderBufferHandle> RboStore { get; }
    public ResourceStore<UniformBufferId, UniformBufferMeta, GlUniformBufferHandle>  UboStore { get; }

    public GlResourceStoreView(ResourceStore<TextureId, TextureMeta, GlTextureHandle> textureStore,
        ResourceStore<ShaderId, ShaderMeta, GlShaderHandle> shaderStore,
        ResourceStore<MeshId, MeshMeta, GlMeshHandle> meshStore,
        ResourceStore<VertexBufferId, VertexBufferMeta, GlVertexBufferHandle> vboStore,
        ResourceStore<IndexBufferId, IndexBufferMeta, GlIndexBufferHandle> iboStore,
        ResourceStore<FrameBufferId, FrameBufferMeta, GlFrameBufferHandle> fboStore,
        ResourceStore<RenderBufferId, RenderBufferMeta, GlRenderBufferHandle> rboStore, 
        ResourceStore<UniformBufferId, UniformBufferMeta, GlUniformBufferHandle> uboStore, MeshRegistry meshRegistry, ShaderRegistry shaderRegistry)
    {
        TextureStore = textureStore;
        ShaderStore = shaderStore;
        MeshStore = meshStore;
        VboStore = vboStore;
        IboStore = iboStore;
        FboStore = fboStore;
        RboStore = rboStore;
        UboStore = uboStore;
        MeshRegistry = meshRegistry;
        ShaderRegistry = shaderRegistry;
    }
}