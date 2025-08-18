#region

using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal class GlResourceStore
{
    private const int storeTier1 = 64;
    private const int storeTier2 = 32;
    private const int storeTier3 = 16;

    public readonly ResourceStore<TextureId, TextureMeta, GlTextureHandle> _textureStore = new(
        initialCapacity: storeTier1,
        i => new TextureId(i + 1),
        id => id.Id - 1
    );

    public readonly ResourceStore<ShaderId, ShaderMeta, GlShaderHandle> _shaderStore = new(
        initialCapacity: storeTier2,
        i => new ShaderId(i + 1),
        id => id.Id - 1
    );

    public readonly ResourceStore<MeshId, MeshMeta, GlMeshHandle> _meshStore = new(
        initialCapacity: storeTier2,
        i => new MeshId(i + 1),
        id => id.Id - 1
    );

    public readonly ResourceStore<VertexBufferId, VertexBufferMeta, GlVertexBufferHandle> _vboStore = new(
        initialCapacity: storeTier2,
        i => new VertexBufferId(i + 1),
        id => id.Id - 1
    );

    public readonly ResourceStore<IndexBufferId, IndexBufferMeta, GlIndexBufferHandle> _iboStore = new(
        initialCapacity: storeTier2,
        i => new IndexBufferId(i + 1),
        id => id.Id - 1
    );

    public readonly ResourceStore<FrameBufferId, FrameBufferMeta, GlFrameBufferHandle> _fboStore = new(
        initialCapacity: storeTier3,
        i => new FrameBufferId(i + 1),
        id => id.Id - 1
    );

    public readonly ResourceStore<RenderBufferId, RenderBufferMeta, GlRenderBufferHandle> _rboStore = new(
        initialCapacity: storeTier3,
        i => new RenderBufferId(i + 1),
        id => id.Id - 1
    );
}