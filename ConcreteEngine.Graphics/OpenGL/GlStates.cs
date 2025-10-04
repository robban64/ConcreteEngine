#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlStates : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendOpsHub _store;

    internal GlStates(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _store = ctx.Store;
    }


    public void ClearColorBuffer(Color4 color, ClearBufferFlag flags)
    {
        _gl.ClearColor(color.R, color.G, color.B, 1);
        _gl.Clear(flags.ToGlEnum());
    }

    public void ClearColor(Color4 color) => _gl.ClearColor(color.R, color.G, color.B, 1);
    public void ClearBuffer(ClearBufferFlag flags) => _gl.Clear(flags.ToGlEnum());
    public void ColorMask(bool v) => _gl.ColorMask(v, v, v, v);

    public void ToggleFrameBufferSrgb(bool enabled)
    {
        if (enabled) _gl.Enable(GLEnum.FramebufferSrgb);
        else _gl.Disable(GLEnum.FramebufferSrgb);
    }

    public void ToggleBlendState(bool enabled)
    {
        if (enabled) _gl.Enable(EnableCap.Blend);
        else _gl.Disable(EnableCap.Blend);
    }

    public void ToggleDepthTest(bool enabled)
    {
        if (enabled) _gl.Enable(EnableCap.DepthTest);
        else _gl.Disable(EnableCap.DepthTest);
    }

    public void ToggleCullFace(bool enabled)
    {
        if (enabled) _gl.Enable(EnableCap.CullFace);
        else _gl.Disable(EnableCap.CullFace);
    }

    public void ToggleDepthMask(bool enabled)
    {
        _gl.DepthMask(enabled);
    }

    public void ToggleScissorTest(bool enabled)
    {
        if (enabled) _gl.Enable(EnableCap.ScissorTest);
        else _gl.Disable(EnableCap.ScissorTest);
    }

    public void SetViewport(Bounds2D viewport) =>
        _gl.Viewport(viewport.X, viewport.Y, (uint)viewport.Width, (uint)viewport.Height);

    public void SetBlendMode(BlendMode blendMode)
    {
        if (blendMode == BlendMode.Unset)
        {
            return;
        }

        var (eq, src, dst) = blendMode.ToGlEnum();
        _gl.BlendEquation(eq);
        _gl.BlendFunc(src, dst);
    }

    public void SetDepthMode(DepthMode depthMode)
    {
        if (depthMode == DepthMode.None)
        {
            return;
        }

        var func = depthMode.ToGlEnum();
        _gl.DepthFunc(func);
    }

    public void SetCullMode(CullMode cullMode)
    {
        if (cullMode == CullMode.Unset)
        {
            return;
        }

        var (face, front) = cullMode.ToGlEnum();
        _gl.CullFace(face);
        _gl.FrontFace(front);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindTexture(in GfxRefToken<TextureId> texRef, int slot) =>
        _gl.BindTextureUnit((uint)slot, _store.Texture.GetRef(texRef).Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindTextureSlot(int slot) => _gl.BindTextureUnit(0, (uint)slot);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindFrameBuffer(GfxRefToken<FrameBufferId> fboRef) =>
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _store.FrameBuffer.GetRef(fboRef).Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindFrameBuffer() => _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindMesh(GfxRefToken<MeshId> mesh) => _gl.BindVertexArray(_store.VertexArray.GetRef(mesh).Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindMesh() => _gl.BindVertexArray(0);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawArrays(DrawPrimitive primitive, int drawCount)
    {
        _gl.DrawArrays(primitive.ToGlEnum(), 0, (uint)drawCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void DrawElements(DrawPrimitive primitive, DrawElementSize elementSize, int drawCount)
    {
        _gl.DrawElements(primitive.ToGlEnum(), (uint)drawCount, elementSize.ToGlEnum(), (void*)0);
    }
}