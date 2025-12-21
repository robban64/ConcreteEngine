using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources.Handles;
using static ConcreteEngine.Graphics.Configuration.GfxLimits;

namespace ConcreteEngine.Graphics.Gfx.Resources.Stores;

internal sealed class GfxStoreHub
{
    internal GfxStoreHub()
    {
    }

    internal GfxResourceStore<TId, TMeta> GetStore<TId, TMeta>()
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        var store = GetStore(TId.Kind);
        if (store is GfxResourceStore<TId, TMeta> typed) return typed;

        ThrowInvalidStoreType(TId.Kind, typeof(TId), typeof(TMeta));
        return null!;
    }

    internal IGfxResourceStore<TId> GetStore<TId>() where TId : unmanaged, IResourceId
    {
        var store = GetStore(TId.Kind);
        if (store is IGfxResourceStore<TId> typed) return typed;

        ThrowInvalidStoreType(TId.Kind, typeof(TId));
        return null!;
    }

    internal IGfxMetaResourceStore<TMeta> GetMetaStore<TMeta>(ResourceKind kind) where TMeta : unmanaged, IResourceMeta
    {
        var store = GetStore(kind);
        if (store is IGfxMetaResourceStore<TMeta> typed) return typed;

        ThrowInvalidStoreType(kind, typeof(TMeta));
        return null!;
    }

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

    internal void RemoveResource(int idValue, ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Texture: TextureStore.Remove(new TextureId(idValue)); break;
            case ResourceKind.Shader: ShaderStore.Remove(new ShaderId(idValue)); break;
            case ResourceKind.Mesh: MeshStore.Remove(new MeshId(idValue)); break;
            case ResourceKind.VertexBuffer: VboStore.Remove(new VertexBufferId(idValue)); break;
            case ResourceKind.IndexBuffer: IboStore.Remove(new IndexBufferId(idValue)); break;
            case ResourceKind.FrameBuffer: FboStore.Remove(new FrameBufferId(idValue)); break;
            case ResourceKind.RenderBuffer: RboStore.Remove(new RenderBufferId(idValue)); break;
            case ResourceKind.UniformBuffer: UboStore.Remove(new UniformBufferId(idValue)); break;
            case ResourceKind.Invalid:
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Invalid resource kind.");
        }
    }

    public readonly TextureStore TextureStore = new(LargeCapacity);
    public readonly ShaderStore ShaderStore = new(MediumCapacity);
    public readonly MeshStore MeshStore = new(LargeCapacity);
    public readonly VboStore VboStore = new(LargeCapacity);
    public readonly IboStore IboStore = new(LargeCapacity);
    public readonly FboStore FboStore = new(LowCapacity);
    public readonly RboStore RboStore = new(LowCapacity);
    public readonly UboStore UboStore = new(LowCapacity);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    [StackTraceHidden]
    private static void ThrowInvalidStoreType(ResourceKind kind, Type id, Type? meta = null) =>
        throw new ArgumentException($"Gfx Store {kind} is not: {id.Name}  {meta?.Name}");
}