using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using static ConcreteEngine.Graphics.Configuration.GfxLimits;

namespace ConcreteEngine.Graphics.Resources;

// ReSharper disable StaticMemberInGenericType
public static class GfxRegistry
{
    private static readonly IGfxResourceStore[] GfxStores = new IGfxResourceStore[GfxMetrics.StoreCount];

    private static class Store<TMeta> where TMeta : unmanaged, IResourceMeta
    {
        public static readonly GfxResourceStore<TMeta> Gfx = new(GetCapacity(TMeta.ResourceKind));
    }
    


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static GfxResourceStore<TMeta> GetStore<TMeta>() where TMeta : unmanaged, IResourceMeta =>
        Store<TMeta>.Gfx;

    internal static IGfxResourceStore GetStore(GraphicsKind kind) => GfxStores[(int)kind - 1];
    
    internal static GfxResourceStore<TextureMeta> TextureStore => Store<TextureMeta>.Gfx;
    internal static GfxResourceStore<ShaderMeta> ShaderStore => Store<ShaderMeta>.Gfx;
    internal static GfxResourceStore<MeshMeta> MeshStore => Store<MeshMeta>.Gfx;
    internal static GfxResourceStore<VertexBufferMeta> VboStore => Store<VertexBufferMeta>.Gfx;
    internal static GfxResourceStore<IndexBufferMeta> IboStore => Store<IndexBufferMeta>.Gfx;
    internal static GfxResourceStore<FrameBufferMeta> FboStore => Store<FrameBufferMeta>.Gfx;
    internal static GfxResourceStore<RenderBufferMeta> RboStore => Store<RenderBufferMeta>.Gfx;
    internal static GfxResourceStore<UniformBufferMeta> UboStore => Store<UniformBufferMeta>.Gfx;


    internal static void CreateStores()
    {
        CreateStore<TextureMeta>();
        CreateStore<ShaderMeta>();
        CreateStore<MeshMeta>();
        CreateStore<VertexBufferMeta>();
        CreateStore<IndexBufferMeta>();
        CreateStore<FrameBufferMeta>();
        CreateStore<RenderBufferMeta>();
        CreateStore<UniformBufferMeta>();
    }

    internal static void DisposeAllStores()
    {
        foreach (var store in GfxStores) store.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateStore<TMeta>() where TMeta : unmanaged, IResourceMeta
    {
        var index = (int)TMeta.ResourceKind - 1;
        if (GfxStores[index] != null!)
            Throwers.InvalidOperation($"Store {nameof(TMeta)} already initialized");

        GfxStores[index] = Store<TMeta>.Gfx;
    }


    private static int GetCapacity(GraphicsKind kind)
    {
        return kind switch
        {
            GraphicsKind.Texture => LargeCapacity,
            GraphicsKind.Shader => MediumCapacity,
            GraphicsKind.Mesh => LargeCapacity,
            GraphicsKind.VertexBuffer => LargeCapacity,
            GraphicsKind.IndexBuffer => LargeCapacity,
            GraphicsKind.UniformBuffer => LowCapacity,
            GraphicsKind.FrameBuffer => LowCapacity,
            GraphicsKind.RenderBuffer => LowCapacity,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}