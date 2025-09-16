using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Resources;

internal class ResourceTypeConverter
{
    private static readonly Dictionary<Type, ResourceKind> ResourceIdToKind = new()
    {
        [typeof(TextureId)]      = ResourceKind.Texture,
        [typeof(ShaderId)]       = ResourceKind.Shader,
        [typeof(MeshId)]         = ResourceKind.Mesh,
        [typeof(VertexBufferId)] = ResourceKind.VertexBuffer,
        [typeof(IndexBufferId)]  = ResourceKind.IndexBuffer,
        [typeof(FrameBufferId)]  = ResourceKind.FrameBuffer,
        [typeof(RenderBufferId)] = ResourceKind.RenderBuffer,
        [typeof(UniformBufferId)]= ResourceKind.UniformBuffer,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ResourceKind Of<TId>() where TId : unmanaged, IResourceId
        => ResourceIdToKind[typeof(TId)];

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TId MakeId<TId>(int rawId) where TId : unmanaged, IResourceId
    {
        if (typeof(TId) == typeof(TextureId))
        {
            var v = new TextureId(rawId);
            return Unsafe.As<TextureId, TId>(ref v);
        }

        if (typeof(TId) == typeof(ShaderId))
        {
            var v = new ShaderId(rawId);
            return Unsafe.As<ShaderId, TId>(ref v);
        }

        if (typeof(TId) == typeof(MeshId))
        {
            var v = new MeshId(rawId);
            return Unsafe.As<MeshId, TId>(ref v);
        }

        if (typeof(TId) == typeof(VertexBufferId))
        {
            var v = new VertexBufferId(rawId);
            return Unsafe.As<VertexBufferId, TId>(ref v);
        }

        if (typeof(TId) == typeof(IndexBufferId))
        {
            var v = new IndexBufferId(rawId);
            return Unsafe.As<IndexBufferId, TId>(ref v);
        }

        if (typeof(TId) == typeof(FrameBufferId))
        {
            var v = new FrameBufferId(rawId);
            return Unsafe.As<FrameBufferId, TId>(ref v);
        }

        if (typeof(TId) == typeof(RenderBufferId))
        {
            var v = new RenderBufferId(rawId);
            return Unsafe.As<RenderBufferId, TId>(ref v);
        }

        if (typeof(TId) == typeof(UniformBufferId))
        {
            var v = new UniformBufferId(rawId);
            return Unsafe.As<UniformBufferId, TId>(ref v);
        }

        throw new NotSupportedException($"Unsupported rawId type: {typeof(TId).Name}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static THandle MakeHandle<THandle>(uint rawHandle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (typeof(THandle) == typeof(GlTextureHandle))
        {
            var v = new GlTextureHandle(rawHandle);
            return Unsafe.As<GlTextureHandle, THandle>(ref v);
        }

        if (typeof(THandle) == typeof(GlShaderHandle))
        {
            var v = new GlShaderHandle(rawHandle);
            return Unsafe.As<GlShaderHandle, THandle>(ref v);
        }

        if (typeof(THandle) == typeof(GlMeshHandle))
        {
            var v = new GlMeshHandle(rawHandle);
            return Unsafe.As<GlMeshHandle, THandle>(ref v);
        }

        if (typeof(THandle) == typeof(GlVboHandle))
        {
            var v = new GlVboHandle(rawHandle);
            return Unsafe.As<GlVboHandle, THandle>(ref v);
        }

        if (typeof(THandle) == typeof(GlIboHandle))
        {
            var v = new GlIboHandle(rawHandle);
            return Unsafe.As<GlIboHandle, THandle>(ref v);
        }

        if (typeof(THandle) == typeof(GlFboHandle))
        {
            var v = new GlFboHandle(rawHandle);
            return Unsafe.As<GlFboHandle, THandle>(ref v);
        }

        if (typeof(THandle) == typeof(GlRboHandle))
        {
            var v = new GlRboHandle(rawHandle);
            return Unsafe.As<GlRboHandle, THandle>(ref v);
        }

        if (typeof(THandle) == typeof(GlUboHandle))
        {
            var v = new GlUboHandle(rawHandle);
            return Unsafe.As<GlUboHandle, THandle>(ref v);
        }

        throw new NotSupportedException($"Unsupported rawHandle type: {typeof(THandle).Name}");
    }

    public static ResourceKind FromHandle<THandle>() where THandle : unmanaged, IResourceHandle
    {
        return typeof(THandle) switch
        {
            var t when t == typeof(GlTextureHandle) => ResourceKind.Texture,
            var t when t == typeof(GlShaderHandle) => ResourceKind.Shader,
            var t when t == typeof(GlMeshHandle) => ResourceKind.Mesh,
            var t when t == typeof(GlVboHandle) => ResourceKind.VertexBuffer,
            var t when t == typeof(GlIboHandle) => ResourceKind.IndexBuffer,
            var t when t == typeof(GlFboHandle) => ResourceKind.FrameBuffer,
            var t when t == typeof(GlRboHandle) => ResourceKind.RenderBuffer,
            var t when t == typeof(GlUboHandle) => ResourceKind.UniformBuffer,
            _ => ResourceKind.Invalid
        };
    }

    public static ResourceKind FromId<TId>() where TId : unmanaged, IResourceId
    {
        return typeof(TId) switch
        {
            var t when t == typeof(TextureId) => ResourceKind.Texture,
            var t when t == typeof(ShaderId) => ResourceKind.Shader,
            var t when t == typeof(MeshId) => ResourceKind.Mesh,
            var t when t == typeof(VertexBufferId) => ResourceKind.VertexBuffer,
            var t when t == typeof(IndexBufferId) => ResourceKind.IndexBuffer,
            var t when t == typeof(FrameBufferId) => ResourceKind.FrameBuffer,
            var t when t == typeof(RenderBufferId) => ResourceKind.RenderBuffer,
            var t when t == typeof(UniformBufferId) => ResourceKind.UniformBuffer,
            _ => ResourceKind.Invalid
        };
    }
}