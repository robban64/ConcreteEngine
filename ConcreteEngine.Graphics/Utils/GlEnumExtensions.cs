#region

using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.Utils;

public static class GlEnumExtensions
{
    public static PrimitiveType ToGlEnum(this DrawPrimitive value)
    {
        return value switch
        {
            DrawPrimitive.Triangles => PrimitiveType.Triangles,
            DrawPrimitive.TriangleStrip => PrimitiveType.TriangleStrip,
            DrawPrimitive.TriangleFan => PrimitiveType.TriangleFan,
            DrawPrimitive.Points => PrimitiveType.Points,
            DrawPrimitive.Lines => PrimitiveType.Lines,
            DrawPrimitive.LineLoop => PrimitiveType.LineLoop,
            DrawPrimitive.LineStrip => PrimitiveType.LineStrip,
            _ => throw  GraphicsException.UnsupportedFeature(nameof(value))
        };
    }
    public static DrawElementsType ToGlEnum(this IboElementType value)
    {
        return value switch
        {
            IboElementType.UnsignedByte => DrawElementsType.UnsignedByte,
            IboElementType.UnsignedShort => DrawElementsType.UnsignedShort,
            IboElementType.UnsignedInt => DrawElementsType.UnsignedInt,
            _ => throw  GraphicsException.UnsupportedFeature($"Index Element Type {value}")
        };
    }
    
    public static ClearBufferMask ToGlEnum(this ClearBufferFlag flags)
    {
        return flags switch
        {
            ClearBufferFlag.None =>  ClearBufferMask.None,
            ClearBufferFlag.Color => ClearBufferMask.ColorBufferBit,
            ClearBufferFlag.Depth => ClearBufferMask.DepthBufferBit,
            ClearBufferFlag.ColorAndDepth => ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit,
            _ => throw GraphicsException.UnsupportedFeature(nameof(flags))
        };
    }
    
    public static (PixelFormat glFormat, InternalFormat glInternalFormat) ToGlEnums(this EnginePixelFormat format)
    {
        return format switch
        {
            EnginePixelFormat.Red => (PixelFormat.Red, InternalFormat.R8),
            EnginePixelFormat.Rgb => (PixelFormat.Rgb, InternalFormat.Rgb8),
            EnginePixelFormat.Rgba => (PixelFormat.Rgba, InternalFormat.Rgba8),
            _ => throw GraphicsException.UnsupportedFeature(nameof(format))
        };
    }

    public static BufferUsageARB ToGlEnum(this BufferUsage usage)
    {
        return usage switch
        {
            BufferUsage.StaticDraw => BufferUsageARB.StaticDraw,
            BufferUsage.DynamicDraw => BufferUsageARB.DynamicDraw,
            BufferUsage.StreamDraw => BufferUsageARB.StreamDraw,
            _ => throw GraphicsException.UnsupportedFeature(nameof(usage))
        };
    }

    public static BufferTargetARB ToGlEnum(this BufferTarget target)
    {
        return target switch
        {
            BufferTarget.VertexBuffer => BufferTargetARB.ArrayBuffer,
            BufferTarget.IndexBuffer => BufferTargetARB.ElementArrayBuffer,
            _ => throw GraphicsException.UnsupportedFeature(nameof(target))
        };
    }

    public static VertexAttribPointerType PrimitiveToVertexAttribPointerType<R>() where R : unmanaged
    {
        if (typeof(R) == typeof(int)) return VertexAttribPointerType.Int;
        if (typeof(R) == typeof(uint)) return VertexAttribPointerType.UnsignedInt;
        if (typeof(R) == typeof(float)) return VertexAttribPointerType.Float;
        if (typeof(R) == typeof(double)) return VertexAttribPointerType.Double;
        if (typeof(R) == typeof(byte)) return VertexAttribPointerType.Byte;

        throw GraphicsException.UnsupportedFeature($"VertexAttribPointerType of type {typeof(R).Name}.");
    }
}