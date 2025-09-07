#region

using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlContextBindingView
{
    public ResourceStore<TextureId, TextureMeta, GlTextureHandle> TextureStore { get; }
    public ResourceStore<ShaderId, ShaderMeta, GlShaderHandle> ShaderStore { get; }
    public ResourceStore<MeshId, MeshMeta, GlMeshHandle> MeshStore { get; }
    public ResourceStore<VertexBufferId, VertexBufferMeta, GlVertexBufferHandle> VboStore { get; }
    public ResourceStore<IndexBufferId, IndexBufferMeta, GlIndexBufferHandle> IboStore { get; }
    public ResourceStore<FrameBufferId, FrameBufferMeta, GlFrameBufferHandle> FboStore { get; }
    public ResourceStore<RenderBufferId, RenderBufferMeta, GlRenderBufferHandle> RboStore { get; }

    public ResourceStore<UniformBufferId, UniformBufferMeta, GlUniformBufferHandle>  UboStore { get; }

    public GlContextBindingView(ResourceStore<TextureId, TextureMeta, GlTextureHandle> textureStore,
        ResourceStore<ShaderId, ShaderMeta, GlShaderHandle> shaderStore,
        ResourceStore<MeshId, MeshMeta, GlMeshHandle> meshStore,
        ResourceStore<VertexBufferId, VertexBufferMeta, GlVertexBufferHandle> vboStore,
        ResourceStore<IndexBufferId, IndexBufferMeta, GlIndexBufferHandle> iboStore,
        ResourceStore<FrameBufferId, FrameBufferMeta, GlFrameBufferHandle> fboStore,
        ResourceStore<RenderBufferId, RenderBufferMeta, GlRenderBufferHandle> rboStore, 
        ResourceStore<UniformBufferId, UniformBufferMeta, GlUniformBufferHandle> uboStore)
    {
        TextureStore = textureStore;
        ShaderStore = shaderStore;
        MeshStore = meshStore;
        VboStore = vboStore;
        IboStore = iboStore;
        FboStore = fboStore;
        RboStore = rboStore;
        UboStore = uboStore;
    }
}