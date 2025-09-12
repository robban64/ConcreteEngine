namespace ConcreteEngine.Graphics.Resources;

internal interface IResourceManager
{
    public ResourceStore<TextureId, TextureMeta> TextureStore { get; }

    public ResourceStore<ShaderId, ShaderMeta> ShaderStore { get; }

    public ResourceStore<MeshId, MeshMeta> MeshStore { get; }

    public ResourceStore<VertexBufferId, VertexBufferMeta> VboStore { get; }

    public ResourceStore<IndexBufferId, IndexBufferMeta> IboStore { get; }

    public ResourceStore<FrameBufferId, FrameBufferMeta> FboStore { get; }

    public ResourceStore<RenderBufferId, RenderBufferMeta> RboStore { get; }

    public ResourceStore<UniformBufferId, UniformBufferMeta> UboStore { get; }

}
internal sealed class ResourceManager : IResourceManager
{
    private const int StoreTier1 = 64;
    private const int StoreTier2 = 32;
    private const int StoreTier3 = 16;

    public ResourceStore<TextureId, TextureMeta> TextureStore { get; } =
        new(initialCapacity: StoreTier1, static i => new TextureId(i + 1));

    public ResourceStore<ShaderId, ShaderMeta> ShaderStore { get; }
        = new(initialCapacity: StoreTier2, static i => new ShaderId(i + 1));

    public ResourceStore<MeshId, MeshMeta> MeshStore { get; }
        = new(initialCapacity: StoreTier2, static i => new MeshId(i + 1));

    public ResourceStore<VertexBufferId, VertexBufferMeta> VboStore { get; }
        = new(initialCapacity: StoreTier2, static i => new VertexBufferId(i + 1));

    public ResourceStore<IndexBufferId, IndexBufferMeta> IboStore { get; }
        = new(initialCapacity: StoreTier2, static i => new IndexBufferId(i + 1));

    public ResourceStore<FrameBufferId, FrameBufferMeta> FboStore { get; }
        = new(initialCapacity: StoreTier3, static i => new FrameBufferId(i + 1));

    public ResourceStore<RenderBufferId, RenderBufferMeta> RboStore { get; }
        = new(initialCapacity: StoreTier3, static i => new RenderBufferId(i + 1));

    public ResourceStore<UniformBufferId, UniformBufferMeta> UboStore { get; }
        = new(initialCapacity: StoreTier3, static i => new UniformBufferId(i + 1));
}