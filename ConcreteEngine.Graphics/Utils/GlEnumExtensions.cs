#region

using ConcreteEngine.Graphics.Error;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.Utils;

public static class GlEnumExtensions
{
    public static (bool enabled, BlendEquationModeEXT eq, BlendingFactor src, BlendingFactor dst) ToGlEnum(
        this BlendMode mode)
    {
        return mode switch
        {
            BlendMode.Alpha => (true, BlendEquationModeEXT.FuncAdd, BlendingFactor.SrcAlpha,
                BlendingFactor.OneMinusSrcAlpha),
            BlendMode.PremultipliedAlpha => (true, BlendEquationModeEXT.FuncAdd, BlendingFactor.One,
                BlendingFactor.OneMinusSrcAlpha),
            BlendMode.Additive => (true, BlendEquationModeEXT.FuncAdd, BlendingFactor.One, BlendingFactor.One),
            _ => (false, BlendEquationModeEXT.FuncAdd, BlendingFactor.One, BlendingFactor.Zero)
        };
    }


    public static (EnableCap cap, DepthFunction func, bool mask) ToGlEnum(this DepthMode preset)
    {
        return preset switch
        {
            DepthMode.Disabled => (EnableCap.DepthTest, DepthFunction.Always, false),
            DepthMode.ReadOnlyLequal => (EnableCap.DepthTest, DepthFunction.Lequal, false),
            DepthMode.WriteLequal => (EnableCap.DepthTest, DepthFunction.Lequal, true),
            DepthMode.WriteLess => (EnableCap.DepthTest, DepthFunction.Less, true),
            _ => (EnableCap.DepthTest, DepthFunction.Always, true)
        };
    }

    public static (EnableCap cap, TriangleFace face, FrontFaceDirection front) ToGlEnum(this CullMode preset)
    {
        return preset switch
        {
            CullMode.None => (EnableCap.CullFace, TriangleFace.FrontAndBack, FrontFaceDirection.Ccw),
            CullMode.BackCcw => (EnableCap.CullFace, TriangleFace.Back, FrontFaceDirection.Ccw),
            CullMode.BackCw => (EnableCap.CullFace, TriangleFace.Back, FrontFaceDirection.CW),
            CullMode.FrontCcw => (EnableCap.CullFace, TriangleFace.Front, FrontFaceDirection.Ccw),
            CullMode.FrontCw => (EnableCap.CullFace, TriangleFace.Front, FrontFaceDirection.CW),
            _ => (EnableCap.CullFace, TriangleFace.Back, FrontFaceDirection.Ccw)
        };
    }

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
        };
    }

    public static DrawElementsType ToGlEnum(this DrawElementType value)
    {
        return value switch
        {
            DrawElementType.UnsignedByte => DrawElementsType.UnsignedByte,
            DrawElementType.UnsignedShort => DrawElementsType.UnsignedShort,
            DrawElementType.UnsignedInt => DrawElementsType.UnsignedInt,
        };
    }

    public static ClearBufferMask ToGlEnum(this ClearBufferFlag flags)
    {
        return flags switch
        {
            ClearBufferFlag.None => ClearBufferMask.None,
            ClearBufferFlag.Color => ClearBufferMask.ColorBufferBit,
            ClearBufferFlag.Depth => ClearBufferMask.DepthBufferBit,
            ClearBufferFlag.ColorAndDepth => ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit,
        };
    }

    public static (PixelFormat glFormat, InternalFormat glInternalFormat) ToGlEnums(this EnginePixelFormat format)
    {
        return format switch
        {
            EnginePixelFormat.Red => (PixelFormat.Red, InternalFormat.R8),
            EnginePixelFormat.Rgb => (PixelFormat.Rgb, InternalFormat.Rgb8),
            EnginePixelFormat.Rgba => (PixelFormat.Rgba, InternalFormat.Rgba8),
        };
    }

    public static BufferUsageARB ToGlEnum(this BufferUsage usage)
    {
        return usage switch
        {
            BufferUsage.StaticDraw => BufferUsageARB.StaticDraw,
            BufferUsage.DynamicDraw => BufferUsageARB.DynamicDraw,
            BufferUsage.StreamDraw => BufferUsageARB.StreamDraw,
        };
    }

    public static BufferTargetARB ToGlEnum(this BufferTarget target)
    {
        return target switch
        {
            BufferTarget.VertexBuffer => BufferTargetARB.ArrayBuffer,
            BufferTarget.IndexBuffer => BufferTargetARB.ElementArrayBuffer,
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