#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal sealed class GfxStoreHub
{
    private const int LargeCapacity = 64;
    private const int MediumCapacity = 32;
    private const int LowCapacity = 16;

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


    public TextureStore TextureStore { get; } = new(LargeCapacity, static i => new TextureId(i + 1));
    public ShaderStore ShaderStore { get; } = new(MediumCapacity, static i => new ShaderId(i + 1));
    public MeshStore MeshStore { get; } = new(MediumCapacity, static i => new MeshId(i + 1));
    public VboStore VboStore { get; } = new(MediumCapacity, static i => new VertexBufferId(i + 1));
    public IboStore IboStore { get; } = new(MediumCapacity, static i => new IndexBufferId(i + 1));
    public FboStore FboStore { get; } = new(LowCapacity, static i => new FrameBufferId(i + 1));
    public RboStore RboStore { get; } = new(LowCapacity, static i => new RenderBufferId(i + 1));
    public UboStore UboStore { get; } = new(LowCapacity, static i => new UniformBufferId(i + 1));


    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    [StackTraceHidden]
    private static void ThrowInvalidStoreType(ResourceKind kind, Type id, Type? meta = null) =>
        throw new ArgumentException($"Gfx Store {kind} is not: {id.Name}  {meta?.Name}");
}