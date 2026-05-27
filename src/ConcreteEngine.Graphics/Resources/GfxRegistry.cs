using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Handles;
using static ConcreteEngine.Graphics.Configuration.GfxLimits;

namespace ConcreteEngine.Graphics.Resources;

// ReSharper disable StaticMemberInGenericType
public static class GfxRegistry
{
    private static readonly IGfxResourceStore[] GfxStores = new IGfxResourceStore[GfxMetrics.StoreCount];
    private static readonly BackendResourceStore[] BackendStores = new BackendResourceStore[GfxMetrics.StoreCount];

    private static class Store<TMeta> where TMeta : unmanaged, IResourceMeta
    {
        public static readonly GfxResourceStore<TMeta> Gfx = new(GetCapacity(TMeta.ResourceKind));
        public static readonly BackendResourceStore Backend = new(GetCapacity(TMeta.ResourceKind), TMeta.ResourceKind);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static GfxResourceStore<TMeta> GetGfxStore<TMeta>() 
        where TMeta : unmanaged, IResourceMeta => Store<TMeta>.Gfx;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static BackendResourceStore GetBackendStore<TMeta>() 
        where TMeta : unmanaged, IResourceMeta => Store<TMeta>.Backend;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IGfxResourceStore GetGfxStore(GraphicsKind kind) => GfxStores[(int)kind - 1]; 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static BackendResourceStore GetBackendStore(GraphicsKind kind) => BackendStores[(int)kind - 1]; 
    
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
        foreach (var store in BackendStores) store.Dispose();
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateStore<TMeta>() where TMeta : unmanaged, IResourceMeta
    {
        var index = (int)TMeta.ResourceKind - 1;
        if (GfxStores[index] != null! || BackendStores[index] != null!)
            Throwers.InvalidOperation($"Store {nameof(TMeta)} already initialized");

        GfxStores[index] = Store<TMeta>.Gfx;
        BackendStores[index] = Store<TMeta>.Backend;
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