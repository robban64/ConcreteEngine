using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.Utils;

internal static class GfxEnumUtils
{
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasBufferAccess(this BufferAccess a, BufferAccess b) => (a & b) != 0;

    
    public static VertexAttribType PrimitiveToVertexAttribPointerType<R>() where R : unmanaged
        => typeof(R) switch
        {
            var t when t == typeof(int)    => VertexAttribType.Int,
            var t when t == typeof(uint)   => VertexAttribType.UnsignedInt,
            var t when t == typeof(float)  => VertexAttribType.Float,
            var t when t == typeof(double) => VertexAttribType.Double,
            var t when t == typeof(byte)   => VertexAttribType.Byte,
            _ => throw new ArgumentOutOfRangeException(nameof(R))
        };
    
    public static uint ToPrimitiveSize(this DrawElementSize t) => t switch
    {
        DrawElementSize.UnsignedByte  =>  (uint)DrawElementSize.UnsignedByte,
        DrawElementSize.UnsignedShort => (uint)DrawElementSize.UnsignedShort,
        DrawElementSize.UnsignedInt   => (uint)DrawElementSize.UnsignedInt,
        _ => throw new ArgumentOutOfRangeException(nameof(t))
    };

    public static DrawElementSize ToDrawElementSize<T>() => typeof(T) switch
    {
        var t when t == typeof(byte)   => DrawElementSize.UnsignedByte,
        var t when t == typeof(ushort) => DrawElementSize.UnsignedShort,
        var t when t == typeof(uint)   => DrawElementSize.UnsignedInt,
        _ => throw new ArgumentOutOfRangeException(typeof(T).Name)
    };

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (bool, uint) ToSamples(this RenderBufferMsaa msaa)
    {
        return msaa switch
        {
            RenderBufferMsaa.None => (false, 0),
            RenderBufferMsaa.X2 => (true, 2u),
            RenderBufferMsaa.X4 => (true, 4u),
            RenderBufferMsaa.X8 => (true, 8u),
            _ => throw new ArgumentOutOfRangeException(nameof(msaa), msaa, null)
        };
    }
    
}