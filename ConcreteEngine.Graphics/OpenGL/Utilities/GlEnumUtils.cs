#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL.Utilities;

internal static class GlEnumUtils
{
    public static VertexAttribType PrimitiveToVertexAttribPointerType<R>() where R : unmanaged =>
        typeof(R) switch
        {
            var t when t == typeof(int) => VertexAttribType.Int,
            var t when t == typeof(uint) => VertexAttribType.UnsignedInt,
            var t when t == typeof(float) => VertexAttribType.Float,
            var t when t == typeof(double) => VertexAttribType.Double,
            var t when t == typeof(byte) => VertexAttribType.Byte,
            _ => throw new ArgumentOutOfRangeException(nameof(R))
        };

    public static BufferStorageMask ToBufferFlag(BufferStorage storage, BufferAccess access)
    {
        BufferStorageMask f = 0;
        if (storage != BufferStorage.Static) f |= BufferStorageMask.DynamicStorageBit;
        if (access.HasBufferAccess(BufferAccess.MapRead)) f |= BufferStorageMask.MapReadBit;
        if (access.HasBufferAccess(BufferAccess.MapWrite)) f |= BufferStorageMask.MapWriteBit;
        if (access.HasBufferAccess(BufferAccess.Persistent)) f |= BufferStorageMask.MapPersistentBit;
        if (access.HasBufferAccess(BufferAccess.Coherent)) f |= BufferStorageMask.MapCoherentBit;

        return f;
    }
}

internal static class GlEnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GLEnum ToGlEnum(this TextureKind textureKind)
    {
        return textureKind switch
        {
            TextureKind.Texture2D => GLEnum.Texture2D,
            TextureKind.Texture3D => GLEnum.Texture3D,
            TextureKind.CubeMap => GLEnum.TextureCubeMap,
            TextureKind.Multisample2D => GLEnum.Texture2DMultisample,
            _ => throw new ArgumentOutOfRangeException(nameof(textureKind))
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GLEnum ToGlAttachmentEnum(this FrameBufferTarget kind)
    {
        return kind switch
        {
            FrameBufferTarget.Color => GLEnum.ColorAttachment0,
            FrameBufferTarget.Depth => GLEnum.DepthAttachment,
            FrameBufferTarget.DepthStencil => GLEnum.DepthStencilAttachment,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InternalFormat ToGlInternalFormatEnum(this FrameBufferTarget kind)
    {
        return kind switch
        {
            FrameBufferTarget.Color => InternalFormat.Rgb8,
            FrameBufferTarget.Depth => InternalFormat.DepthComponent24,
            FrameBufferTarget.DepthStencil => InternalFormat.Depth24Stencil8,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (BlendEquationModeEXT eq, BlendingFactor src, BlendingFactor dst) ToGlEnum(
        this BlendMode mode)
    {
        return mode switch
        {
            BlendMode.Alpha => ( BlendEquationModeEXT.FuncAdd, BlendingFactor.SrcAlpha,
                BlendingFactor.OneMinusSrcAlpha),
            BlendMode.PremultipliedAlpha => (BlendEquationModeEXT.FuncAdd, BlendingFactor.One,
                BlendingFactor.OneMinusSrcAlpha),
            BlendMode.Additive => (BlendEquationModeEXT.FuncAdd, BlendingFactor.One, BlendingFactor.One),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DepthFunction ToGlEnum(this DepthMode preset)
    {
        return preset switch
        {
            DepthMode.None => DepthFunction.Always,
            DepthMode.Lequal => DepthFunction.Lequal,
            DepthMode.Less => DepthFunction.Less,
            DepthMode.Equal => DepthFunction.Equal,
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (TriangleFace face, FrontFaceDirection front) ToGlEnum(this CullMode preset)
    {
        return preset switch
        {
            CullMode.None => ( TriangleFace.FrontAndBack, FrontFaceDirection.Ccw),
            CullMode.BackCcw => ( TriangleFace.Back, FrontFaceDirection.Ccw),
            CullMode.BackCw => ( TriangleFace.Back, FrontFaceDirection.CW),
            CullMode.FrontCcw => ( TriangleFace.Front, FrontFaceDirection.Ccw),
            CullMode.FrontCw => ( TriangleFace.Front, FrontFaceDirection.CW),
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DrawElementsType ToGlEnum(this DrawElementSize value)
    {
        return value switch
        {
            DrawElementSize.UnsignedByte => DrawElementsType.UnsignedByte,
            DrawElementSize.UnsignedShort => DrawElementsType.UnsignedShort,
            DrawElementSize.UnsignedInt => DrawElementsType.UnsignedInt,
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClearBufferMask ToGlEnum(this ClearBufferFlag flags)
    {
        return flags switch
        {
            ClearBufferFlag.None => ClearBufferMask.None,
            ClearBufferFlag.Color => ClearBufferMask.ColorBufferBit,
            ClearBufferFlag.Depth => ClearBufferMask.DepthBufferBit,
            ClearBufferFlag.ColorAndDepth => ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit,
            _ => throw new ArgumentOutOfRangeException(nameof(flags))
        };
    }

    public static SizedInternalFormat ToStorageFormat(this EnginePixelFormat format)
    {
        return format switch
        {
            EnginePixelFormat.Rgb => SizedInternalFormat.Rgb8,
            EnginePixelFormat.Rgba => SizedInternalFormat.Rgba8,
            EnginePixelFormat.SrgbAlpha => SizedInternalFormat.Srgb8Alpha8,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }
    
    public static (PixelFormat fmt, PixelType type) ToUploadFormatType(this EnginePixelFormat f)
    {
        return f switch
        {
            EnginePixelFormat.Rgb => (PixelFormat.Rgb, PixelType.UnsignedByte),
            EnginePixelFormat.Rgba => (PixelFormat.Rgba, PixelType.UnsignedByte),
            // sRGB only for internal format
            EnginePixelFormat.SrgbAlpha => (PixelFormat.Rgba, PixelType.UnsignedByte),
            _ => throw new ArgumentOutOfRangeException(nameof(f))
        };
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BufferUsage ToBufferUsage(this BufferStorage usage)
    {
        return usage switch
        {
            BufferStorage.Static => BufferUsage.StaticDraw,
            BufferStorage.Dynamic => BufferUsage.DynamicDraw,
            BufferStorage.Stream => BufferUsage.StreamDraw,
            _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VertexBufferObjectUsage ToGlEnum(this BufferUsage usage)
    {
        return usage switch
        {
            BufferUsage.StaticDraw => VertexBufferObjectUsage.StaticDraw,
            BufferUsage.DynamicDraw => VertexBufferObjectUsage.DynamicDraw,
            BufferUsage.StreamDraw => VertexBufferObjectUsage.StreamDraw,
            _ => throw new ArgumentOutOfRangeException(nameof(usage))
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BufferTargetARB ToGlEnum(this BufferTarget target)
    {
        return target switch
        {
            BufferTarget.VertexBuffer => BufferTargetARB.ArrayBuffer,
            BufferTarget.IndexBuffer => BufferTargetARB.ElementArrayBuffer,
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
    }
}