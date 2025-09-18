#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.Utils;

internal static class GlEnumUtils
{
    public static BufferStorageMask ToBufferFlag(BufferStorage storage, BufferAccess access)
    {
        BufferStorageMask flags = 0;

        if (storage == BufferStorage.Dynamic)
            flags |= BufferStorageMask.DynamicStorageBit;
        else if (storage == BufferStorage.Stream) 
            flags |= BufferStorageMask.DynamicStorageBit;

        
        //BufferAccess access
        if (access.HasBufferAccess(BufferAccess.MapRead))
            flags |= BufferStorageMask.MapReadBit;
        if (access.HasBufferAccess(BufferAccess.MapWrite))
            flags |= BufferStorageMask.MapWriteBit;
        if (access.HasBufferAccess(BufferAccess.Persistent))
            flags |= BufferStorageMask.MapPersistentBit;
        if (access.HasBufferAccess(BufferAccess.Coherent))
            flags |= BufferStorageMask.MapCoherentBit;

        return flags;
    }

}
internal static class GlEnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GLEnum ToGlEnum(this TextureKind textureKind)
    {
        return textureKind switch
        {
            TextureKind.Texture1D => GLEnum.Texture1D,
            TextureKind.Texture2D => GLEnum.Texture2D,
            TextureKind.Texture3D => GLEnum.Texture3D,
            TextureKind.CubeMap => GLEnum.TextureCubeMap,
            _ => throw new ArgumentOutOfRangeException(nameof(textureKind))
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GLEnum ToGlEnum(this FrameBufferTarget kind)
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


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (EnableCap cap, TriangleFace face, FrontFaceDirection front) ToGlEnum(this CullMode preset)
    {
        return preset switch
        {
            CullMode.None => (EnableCap.CullFace, TriangleFace.FrontAndBack, FrontFaceDirection.Ccw),
            CullMode.BackCcw => (EnableCap.CullFace, TriangleFace.Back, FrontFaceDirection.Ccw),
            CullMode.BackCw => (EnableCap.CullFace, TriangleFace.Back, FrontFaceDirection.CW),
            CullMode.FrontCcw => (EnableCap.CullFace, TriangleFace.Front, FrontFaceDirection.Ccw),
            CullMode.FrontCw => (EnableCap.CullFace, TriangleFace.Front, FrontFaceDirection.CW),
            _ => (EnableCap.CullFace, TriangleFace.Back, FrontFaceDirection.Ccw),
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (PixelFormat glFormat, InternalFormat glInternalFormat) ToGlEnums(this EnginePixelFormat format)
    {
        return format switch
        {
            EnginePixelFormat.Red => (PixelFormat.Red, InternalFormat.R8),
            EnginePixelFormat.Rgb => (PixelFormat.Rgb, InternalFormat.Rgb8),
            EnginePixelFormat.Rgba => (PixelFormat.Rgba, InternalFormat.Rgba8),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
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