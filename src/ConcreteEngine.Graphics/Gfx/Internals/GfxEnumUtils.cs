using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Internals;

internal static class GfxEnumUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasBufferAccess(this BufferAccess a, BufferAccess b) => (a & b) != 0;


    public static int SizeInBytes(this VertexFormat fmt) =>
        fmt switch
        {
            VertexFormat.UByte => 1,
            VertexFormat.UShort => 2,
            VertexFormat.Half => 2,
            VertexFormat.Float => 4,
            VertexFormat.Integer => 4,
            _ => Throwers.Unreachable<int>(nameof(fmt))
        };

    public static int ToPrimitiveSize(this DrawElementSize t) =>
        t switch
        {
            DrawElementSize.UnsignedByte => 1,
            DrawElementSize.UnsignedShort => 2,
            DrawElementSize.UnsignedInt => 4,
            _ => Throwers.Unreachable<int>(nameof(t))
        };

    public static DrawElementSize ToDrawElementSize(int stride) =>
        stride switch
        {
            1 => DrawElementSize.UnsignedByte,
            2 => DrawElementSize.UnsignedShort,
            4 => DrawElementSize.UnsignedInt,
            _ => Throwers.Unreachable<DrawElementSize>(nameof(stride))
        };

    public static int ToAnisotropy(this TextureAnisotropy anisotropy)
    {
        return anisotropy switch
        {
            TextureAnisotropy.Off => 0,
            TextureAnisotropy.X2 => 2,
            TextureAnisotropy.X4 => 4,
            TextureAnisotropy.X8 => 8,
            TextureAnisotropy.X16 => 16,
            _ => Throwers.Unreachable<int>(nameof(anisotropy))
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
            _ => Throwers.Unreachable<int>(nameof(msaa))
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
            _ => Throwers.Unreachable<RenderBufferMsaa>(nameof(samples))
        };
    }
}