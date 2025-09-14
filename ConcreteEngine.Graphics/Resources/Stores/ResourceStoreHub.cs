using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class ResourceStoreHub
{
    private const int StoreTier1 = 64;
    private const int StoreTier2 = 32;
    private const int StoreTier3 = 16;


    public ResourceStoreHub()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IResourceStore GetStore(ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Texture: return TextureStore;
            case ResourceKind.Shader: return ShaderStore;
            case ResourceKind.Mesh: return MeshStore;
            case ResourceKind.VertexBuffer: return VboStore;
            case ResourceKind.IndexBuffer: return IboStore;
            case ResourceKind.FrameBuffer: return FboStore;
            case ResourceKind.RenderBuffer: return RboStore;
            case ResourceKind.UniformBuffer: return UboStore;
            case ResourceKind.Invalid:
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Invalid resource kind.");
        }
    }


    public ResourceStore<TextureId, TextureMeta> TextureStore { get; } =
        new(ResourceKind.Texture, StoreTier1, static i => new TextureId(i + 1));

    public ResourceStore<ShaderId, ShaderMeta> ShaderStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new ShaderId(i + 1));

    public ResourceStore<MeshId, MeshMeta> MeshStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new MeshId(i + 1));

    public ResourceStore<VertexBufferId, VertexBufferMeta> VboStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new VertexBufferId(i + 1));

    public ResourceStore<IndexBufferId, IndexBufferMeta> IboStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new IndexBufferId(i + 1));

    public ResourceStore<FrameBufferId, FrameBufferMeta> FboStore { get; }
        = new(ResourceKind.Texture, StoreTier3, static i => new FrameBufferId(i + 1));

    public ResourceStore<RenderBufferId, RenderBufferMeta> RboStore { get; }
        = new(ResourceKind.Texture, StoreTier3, static i => new RenderBufferId(i + 1));

    public ResourceStore<UniformBufferId, UniformBufferMeta> UboStore { get; }
        = new(ResourceKind.Texture, StoreTier3, static i => new UniformBufferId(i + 1));
}