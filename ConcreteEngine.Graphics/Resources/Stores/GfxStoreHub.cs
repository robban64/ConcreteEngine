using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class GfxStoreHub
{
    private const int StoreTier1 = 64;
    private const int StoreTier2 = 32;
    private const int StoreTier3 = 16;

    internal GfxStoreHub()
    {
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal GfxResourceStore<TId, TMeta> GetStore<TId, TMeta>(ResourceKind kind)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        var store = GetStore(kind);
        if (store is GfxResourceStore<TId, TMeta> typed) return typed;
        
        ThrowInvalidStoreType(kind, typeof(TId), typeof(TMeta));
        return null!;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal IGfxResourceStore<TId> GetStore<TId>(ResourceKind kind) where TId : unmanaged, IResourceId
    {
        var store = GetStore(kind);
        if (store is IGfxResourceStore<TId> typed) return typed;
        
        ThrowInvalidStoreType(kind, typeof(TId));
        return null!;
    }

    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn, StackTraceHidden]
    private static void ThrowInvalidStoreType(ResourceKind kind, Type id, Type? meta = null)
        => throw new ArgumentException($"Gfx Store {kind} is not: {id.Name}  {meta?.Name}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IGfxResourceStore GetStore(ResourceKind kind)
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


    public GfxResourceStore<TextureId, TextureMeta> TextureStore { get; } =
        new(ResourceKind.Texture, StoreTier1, static i => new TextureId(i + 1));

    public GfxResourceStore<ShaderId, ShaderMeta> ShaderStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new ShaderId(i + 1));

    public GfxResourceStore<MeshId, MeshMeta> MeshStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new MeshId(i + 1));

    public GfxResourceStore<VertexBufferId, VertexBufferMeta> VboStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new VertexBufferId(i + 1));

    public GfxResourceStore<IndexBufferId, IndexBufferMeta> IboStore { get; }
        = new(ResourceKind.Texture, StoreTier2, static i => new IndexBufferId(i + 1));

    public GfxResourceStore<FrameBufferId, FrameBufferMeta> FboStore { get; }
        = new(ResourceKind.Texture, StoreTier3, static i => new FrameBufferId(i + 1));

    public GfxResourceStore<RenderBufferId, RenderBufferMeta> RboStore { get; }
        = new(ResourceKind.Texture, StoreTier3, static i => new RenderBufferId(i + 1));

    public GfxResourceStore<UniformBufferId, UniformBufferMeta> UboStore { get; }
        = new(ResourceKind.Texture, StoreTier3, static i => new UniformBufferId(i + 1));
}