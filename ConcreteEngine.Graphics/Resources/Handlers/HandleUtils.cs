#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Graphics.Resources;

internal class HandleUtils
{
    private static readonly Dictionary<Type, ResourceKind> ResourceIdToKind = new()
    {
        [typeof(TextureId)] = ResourceKind.Texture,
        [typeof(ShaderId)] = ResourceKind.Shader,
        [typeof(MeshId)] = ResourceKind.Mesh,
        [typeof(VertexBufferId)] = ResourceKind.VertexBuffer,
        [typeof(IndexBufferId)] = ResourceKind.IndexBuffer,
        [typeof(FrameBufferId)] = ResourceKind.FrameBuffer,
        [typeof(RenderBufferId)] = ResourceKind.RenderBuffer,
        [typeof(UniformBufferId)] = ResourceKind.UniformBuffer
    };


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ResourceKind ToResourceKind<TId>() where TId : unmanaged, IResourceId =>
        ResourceIdToKind[typeof(TId)];

    /*

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
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
          public static THandle MakeHandle<THandle>(uint rawHandle) where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
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
     */
}