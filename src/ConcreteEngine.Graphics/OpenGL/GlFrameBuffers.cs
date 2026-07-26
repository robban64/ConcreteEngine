using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;
using static ConcreteEngine.Graphics.OpenGL.GlDriver;

namespace ConcreteEngine.Graphics.OpenGL;

internal static class GlFrameBuffers
{

    // Fix ClearBufferMask and Filter, depth/stencil use filter = Nearest
    public static void Blit(NativeHandle readFbo, NativeHandle drawFbo,
        Size2D srcSize, Size2D dstSize, bool linear)
    {
        var filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
        Gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
        Gl.DrawBuffer(DrawBufferMode.ColorAttachment0);

        Gl.BlitNamedFramebuffer(
            readFbo, drawFbo,
            0, 0, srcSize.Width, srcSize.Height,
            0, 0, dstSize.Width, dstSize.Height,
            ClearBufferMask.ColorBufferBit, filter
        );
    }

    public static void BlitDefault(NativeHandle readFbo, Size2D srcSize, Size2D dstSize, bool linear)
    {
        var filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
        //Gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
        //Gl.DrawBuffer(DrawBufferMode.ColorAttachment0);

        Gl.BlitNamedFramebuffer(
            readFbo, 0,
            0, 0, srcSize.Width, srcSize.Height,
            0, 0, dstSize.Width, dstSize.Height,
            ClearBufferMask.ColorBufferBit, filter
        );
    }

    public static NativeHandle CreateFrameBuffer()
    {
        Gl.CreateFramebuffers(1, out uint fbo);
        return new NativeHandle(fbo);
    }

    public static NativeHandle CreateRenderBuffer(FrameBufferAttachmentSlot attachment, Size2D size, int samples)
    {
        var internalFormat = attachment.ToGlInternalFormatEnum();
        var (width, height) = size.ToUnsigned();

        Gl.CreateRenderbuffers(1, out uint rbo);
        if (samples > 0)
            Gl.NamedRenderbufferStorageMultisample(rbo, (uint)samples, internalFormat, width, height);
        else
            Gl.NamedRenderbufferStorage(rbo, internalFormat, width, height);

        return new NativeHandle(rbo);
    }

    public static void AttachTexture(NativeHandle fboHandle, NativeHandle textureHandle, FrameBufferAttachmentSlot attachmentSlot)
    {
        var glAttachment = attachmentSlot.ToGlAttachmentEnum();
        Gl.NamedFramebufferTexture(fboHandle, glAttachment, textureHandle, 0);
    }

    public static void AttachRenderBuffer(NativeHandle fboHandle, NativeHandle rboHandle,
        FrameBufferAttachmentSlot attachmentSlot)
    {
        var glAttachment = attachmentSlot.ToGlAttachmentEnum();
        Gl.NamedFramebufferRenderbuffer(fboHandle, glAttachment, RenderbufferTarget.Renderbuffer, rboHandle);
    }

    public static void SetDrawReadBuffer(NativeHandle fboHandle, bool colorAttachment)
    {
        var glEnum = colorAttachment ? GLEnum.ColorAttachment0 : GLEnum.None;
        Gl.NamedFramebufferDrawBuffer(fboHandle, glEnum);
        Gl.NamedFramebufferReadBuffer(fboHandle, glEnum);
    }

    public static void ValidateComplete(NativeHandle fboHandle, bool colorAttachment)
    {
        var glEnum = colorAttachment ? GLEnum.ColorAttachment0 : GLEnum.None;
        Gl.NamedFramebufferDrawBuffer(fboHandle, glEnum);
        Gl.NamedFramebufferReadBuffer(fboHandle, glEnum);

        var status = Gl.CheckNamedFramebufferStatus(fboHandle, GLEnum.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            GraphicsException.ThrowFramebufferIncomplete(nameof(fboHandle), status.ToString());
    }
}