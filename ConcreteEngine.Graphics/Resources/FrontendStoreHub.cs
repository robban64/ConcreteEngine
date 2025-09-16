using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class FrontendStoreHub
{
    private const int StoreTier1 = 64;
    private const int StoreTier2 = 32;
    private const int StoreTier3 = 16;


    internal FrontendStoreHub()
    {
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal FrontendResourceStore<TId, TMeta> GetStore<TId, TMeta>(ResourceKind kind)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        var store = GetStore(kind);
        if (store is FrontendResourceStore<TId, TMeta> typed) return typed;
        throw new ArgumentException($"Frontend Store {kind} is not {typeof(TId).Name} - {typeof(TMeta).Name}");
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal IResourceStore<TId> GetStore<TId>(ResourceKind kind) where TId : unmanaged, IResourceId
    {
        var store = GetStore(kind);
        if (store is IResourceStore<TId> typed) return typed;
        throw new ArgumentException($"Frontend Store {kind} is not {typeof(TId).Name} ");
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


    public FrontendResourceStore<TextureId, TextureMeta> TextureStore { get; } =
        new(ResourceKind.Texture, StoreTier1, static i => new TextureId(i + 1));

    public FrontendResourceStore<ShaderId, ShaderMeta> ShaderStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new ShaderId(i + 1));

    public FrontendResourceStore<MeshId, MeshMeta> MeshStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new MeshId(i + 1));

    public FrontendResourceStore<VertexBufferId, VertexBufferMeta> VboStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new VertexBufferId(i + 1));

    public FrontendResourceStore<IndexBufferId, IndexBufferMeta> IboStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new IndexBufferId(i + 1));

    public FrontendResourceStore<FrameBufferId, FrameBufferMeta> FboStore { get; }
        = new(ResourceKind.Texture, StoreTier3, static i => new FrameBufferId(i + 1));

    public FrontendResourceStore<RenderBufferId, RenderBufferMeta> RboStore { get; }
        = new(ResourceKind.Texture, StoreTier3, static i => new RenderBufferId(i + 1));

    public FrontendResourceStore<UniformBufferId, UniformBufferMeta> UboStore { get; }
        = new(ResourceKind.Texture, StoreTier3, static i => new UniformBufferId(i + 1));
}