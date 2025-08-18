using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.OpenGL;

internal class GlContextBindingView
{
    public readonly ResourceStore<TextureId, TextureMeta, GlTextureHandle> TextureStore;
    public readonly ResourceStore<ShaderId, ShaderMeta, GlShaderHandle> ShaderStore;
    public readonly ResourceStore<MeshId, MeshMeta, GlMeshHandle> MeshStore;
    public readonly ResourceStore<VertexBufferId, VertexBufferMeta, GlVertexBufferHandle> VboStore;
    public readonly ResourceStore<IndexBufferId, IndexBufferMeta, GlIndexBufferHandle> IboStore;
    public readonly ResourceStore<FrameBufferId, FrameBufferMeta, GlFrameBufferHandle> FboStore;
    public readonly ResourceStore<RenderBufferId, RenderBufferMeta, GlRenderBufferHandle> RboStore;


    public GlContextBindingView(ResourceStore<TextureId, TextureMeta, GlTextureHandle> textureStore,
        ResourceStore<ShaderId, ShaderMeta, GlShaderHandle> shaderStore,
        ResourceStore<MeshId, MeshMeta, GlMeshHandle> meshStore,
        ResourceStore<VertexBufferId, VertexBufferMeta, GlVertexBufferHandle> vboStore,
        ResourceStore<IndexBufferId, IndexBufferMeta, GlIndexBufferHandle> iboStore,
        ResourceStore<FrameBufferId, FrameBufferMeta, GlFrameBufferHandle> fboStore,
        ResourceStore<RenderBufferId, RenderBufferMeta, GlRenderBufferHandle> rboStore)
    {
        TextureStore = textureStore;
        ShaderStore = shaderStore;
        MeshStore = meshStore;
        VboStore = vboStore;
        IboStore = iboStore;
        FboStore = fboStore;
        RboStore = rboStore;
    }
/*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetTextureHandle(TextureId id) => TextureStore.GetHandle(id).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetShaderHandle(ShaderId id) => ShaderStore.GetHandle(id).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetMeshHandle(MeshId id) => MeshStore.GetHandle(id).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetIndexBufferHandle(IndexBufferId id) => IboStore.GetHandle(id).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetVertexBufferHandle(VertexBufferId id) => VboStore.GetHandle(id).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetFrameBufferHandle(FrameBufferId id) => FboStore.GetHandle(id).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetRenderBufferHandle(RenderBufferId id) => RboStore.GetHandle(id).Handle;

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TextureMeta GetTextureMeta(TextureId id) => ref TextureStore.GetMeta(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly ShaderMeta GetShaderMeta(ShaderId id) => ref ShaderStore.GetMeta(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly MeshMeta GetMeshMeta(MeshId id) => ref MeshStore.GetMeta(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly IndexBufferMeta GetIndexBufferMeta(IndexBufferId id) => ref IboStore.GetMeta(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly VertexBufferMeta GetVertexBufferMeta(VertexBufferId id) => ref VboStore.GetMeta(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly FrameBufferMeta GetFrameBufferMeta(FrameBufferId id) => ref FboStore.GetMeta(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RenderBufferMeta GetRenderBufferMeta(RenderBufferId id) => ref RboStore.GetMeta(id);
    */
    /*
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetTextureHandleMeta(TextureId id, out TextureMeta meta) => _textureStore.GetHandleAndMeta(id, out meta).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetShaderHandleMeta(ShaderId id) => _shaderStore.GetHandle(id).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetMeshHandleMeta(MeshId id) => _meshStore.GetHandle(id).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetIndexBufferHandleMeta(IndexBufferId id) => _iboStore.GetHandle(id).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetVertexBufferHandleMeta(VertexBufferId id) => _vboStore.GetHandle(id).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetFrameBufferHandleMeta(FrameBufferId id) => _fboStore.GetHandle(id).Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetRenderBufferHandleMeta(RenderBufferId id) => _rboStore.GetHandle(id).Handle;
    */

}