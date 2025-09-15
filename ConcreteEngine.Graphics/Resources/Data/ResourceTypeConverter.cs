using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Resources;

internal class ResourceTypeConverter
{
    public static TextureId CreateTextureId(int i) => new(i + 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static THandle MakeHandle<THandle>(uint handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (typeof(THandle) == typeof(GlTextureHandle))
        {
            var v = new GlTextureHandle(handle);
            return Unsafe.As<GlTextureHandle, THandle>(ref v);
        }
        if (typeof(THandle) == typeof(GlShaderHandle))
        {
            var v = new GlShaderHandle(handle);
            return Unsafe.As<GlShaderHandle, THandle>(ref v);
        }
        if (typeof(THandle) == typeof(GlMeshHandle))
        {
            var v = new GlMeshHandle(handle);
            return Unsafe.As<GlMeshHandle, THandle>(ref v);
        }
        if (typeof(THandle) == typeof(GlVboHandle))
        {
            var v = new GlVboHandle(handle);
            return Unsafe.As<GlVboHandle, THandle>(ref v);
        }
        if (typeof(THandle) == typeof(GlIboHandle))
        {
            var v = new GlIboHandle(handle);
            return Unsafe.As<GlIboHandle, THandle>(ref v);
        }
        if (typeof(THandle) == typeof(GlFboHandle))
        {
            var v = new GlFboHandle(handle);
            return Unsafe.As<GlFboHandle, THandle>(ref v);
        }
        if (typeof(THandle) == typeof(GlRboHandle))
        {
            var v = new GlRboHandle(handle);
            return Unsafe.As<GlRboHandle, THandle>(ref v);
        }
        if (typeof(THandle) == typeof(GlUboHandle))
        {
            var v = new GlUboHandle(handle);
            return Unsafe.As<GlUboHandle, THandle>(ref v);
        }

        throw new NotSupportedException($"Unsupported handle type: {typeof(THandle).Name}");
    }
    
    public static ResourceKind FromHandle<THandle>()
        where THandle : unmanaged, IResourceHandle
        => typeof(THandle) switch
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

    public static ResourceKind FromId<TId>()
        where TId : unmanaged, IResourceId =>
        typeof(TId) switch
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