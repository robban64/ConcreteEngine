#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Internal;

internal static class GfxUtilsEnum
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasBufferAccess(this BufferAccess a, BufferAccess b) => (a & b) != 0;

    public static int SizeInBytes(this VertexFormat fmt) =>
        fmt switch
        {
            VertexFormat.UByte => 1,
            VertexFormat.UShort => 2,
            VertexFormat.Float => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(fmt))
        };

    public static int ToPrimitiveSize(this DrawElementSize t) =>
        t switch
        {
            DrawElementSize.UnsignedByte => 1,
            DrawElementSize.UnsignedShort => 2,
            DrawElementSize.UnsignedInt => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(t))
        };

    public static DrawElementSize ToDrawElementSize<T>() =>
        typeof(T) switch
        {
            var t when t == typeof(byte) => DrawElementSize.UnsignedByte,
            var t when t == typeof(ushort) => DrawElementSize.UnsignedShort,
            var t when t == typeof(uint) => DrawElementSize.UnsignedInt,
            _ => throw new ArgumentOutOfRangeException(typeof(T).Name)
        };

    public static int ToAnisotropy(this TextureAnisotropy msaa)
    {
        return msaa switch
        {
            TextureAnisotropy.Off => 0,
            TextureAnisotropy.Default => 4,
            TextureAnisotropy.X2 => 2,
            TextureAnisotropy.X4 => 4,
            TextureAnisotropy.X8 => 8,
            TextureAnisotropy.X16 => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(msaa), msaa, null)
        };
    }

    public static int ToSamples(this RenderBufferMsaa msaa)
    {
        return msaa switch
        {
            RenderBufferMsaa.None => 0,
            RenderBufferMsaa.X2 => 2,
            RenderBufferMsaa.X4 => 4,
            RenderBufferMsaa.X8 => 8,
            _ => throw new ArgumentOutOfRangeException(nameof(msaa), msaa, null)
        };
    }

    public static RenderBufferMsaa ToRenderBufferMsaa(int? samples)
    {
        return samples switch
        {
            null => RenderBufferMsaa.None,
            0 => RenderBufferMsaa.None,
            2 => RenderBufferMsaa.X2,
            4 => RenderBufferMsaa.X4,
            8 => RenderBufferMsaa.X8,
            _ => throw new ArgumentOutOfRangeException(nameof(samples), samples, null)
        };
    }
}