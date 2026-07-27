using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using static ConcreteEngine.Graphics.Configuration.GfxLimits;

namespace ConcreteEngine.Graphics.Gfx;

// ReSharper disable StaticMemberInGenericType
public static class GfxRegistry
{
    public static int StoreCount => GraphicsKindExt.Count - 1;

    private static readonly IGfxResourceStore[] GfxStores = new IGfxResourceStore[StoreCount];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static GfxStore<TMeta> GetStore<TMeta>() where TMeta : unmanaged, IResourceMeta =>
        GfxStore<TMeta>.Instance;
    
    internal static GfxStore<TextureMeta> TextureStore => GfxStore<TextureMeta>.Instance;
    internal static GfxStore<ShaderMeta> ShaderStore => GfxStore<ShaderMeta>.Instance;
    internal static GfxStore<MeshMeta> MeshStore => GfxStore<MeshMeta>.Instance;
    internal static GfxStore<VertexBufferMeta> VboStore => GfxStore<VertexBufferMeta>.Instance;
    internal static GfxStore<IndexBufferMeta> IboStore => GfxStore<IndexBufferMeta>.Instance;
    internal static GfxStore<FrameBufferMeta> FboStore => GfxStore<FrameBufferMeta>.Instance;
    internal static GfxStore<RenderBufferMeta> RboStore => GfxStore<RenderBufferMeta>.Instance;
    internal static GfxStore<UniformBufferMeta> UboStore => GfxStore<UniformBufferMeta>.Instance;

    internal static IGfxResourceStore GetStore(GraphicsKind kind) => GfxStores[(int)kind - 1];
    internal static ReadOnlySpan<IGfxResourceStore> GetStores() => GfxStores;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeHandle GetHandle<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        return GetStore<TMeta>().GetHandle(id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TMeta GetMeta<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        return GetStore<TMeta>().GetMeta(id);
    }

    public static void BindMetaChanged<TMeta>(Action<int> callback) where TMeta : unmanaged, IResourceMeta
    {
        GetStore<TMeta>().BindOnUpdateCallback(callback);
    }
    
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

        GfxStores[index] = new GfxStore<TMeta>(GetCapacity(TMeta.ResourceKind));
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