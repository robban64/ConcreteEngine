using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Graphics.Gfx.Handles;
using static ConcreteEngine.Graphics.Configuration.GfxLimits;

namespace ConcreteEngine.Graphics.Gfx.Resources;

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

    internal IGfxMetaResourceStore<TMeta> GetMetaStore<TMeta>(GraphicsKind kind)
        where TMeta : unmanaged, IResourceMeta
    {
        var store = GetStore(kind);
        if (store is IGfxMetaResourceStore<TMeta> typed) return typed;

        ThrowInvalidStoreType(kind, typeof(TMeta));
        return null!;
    }

    public IGfxResourceStore GetStore(GraphicsKind kind)
    {
        switch (kind)
        {
            case GraphicsKind.Texture: return TextureStore;
            case GraphicsKind.Shader: return ShaderStore;
            case GraphicsKind.Mesh: return MeshStore;
            case GraphicsKind.VertexBuffer: return VboStore;
            case GraphicsKind.IndexBuffer: return IboStore;
            case GraphicsKind.FrameBuffer: return FboStore;
            case GraphicsKind.RenderBuffer: return RboStore;
            case GraphicsKind.UniformBuffer: return UboStore;
            case GraphicsKind.Invalid:
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Invalid resource kind.");
        }
    }

    internal void RemoveResource(int idValue, GraphicsKind kind)
    {
        switch (kind)
        {
            case GraphicsKind.Texture: TextureStore.Remove(new TextureId(idValue)); break;
            case GraphicsKind.Shader: ShaderStore.Remove(new ShaderId(idValue)); break;
            case GraphicsKind.Mesh: MeshStore.Remove(new MeshId(idValue)); break;
            case GraphicsKind.VertexBuffer: VboStore.Remove(new VertexBufferId(idValue)); break;
            case GraphicsKind.IndexBuffer: IboStore.Remove(new IndexBufferId(idValue)); break;
            case GraphicsKind.FrameBuffer: FboStore.Remove(new FrameBufferId(idValue)); break;
            case GraphicsKind.RenderBuffer: RboStore.Remove(new RenderBufferId(idValue)); break;
            case GraphicsKind.UniformBuffer: UboStore.Remove(new UniformBufferId(idValue)); break;
            case GraphicsKind.Invalid:
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
    private static void ThrowInvalidStoreType(GraphicsKind kind, Type id, Type? meta = null) =>
        throw new ArgumentException($"Gfx Store {kind} is not: {id.Name}  {meta?.Name}");
}