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

    private readonly BackendOps<MeshId, GlMeshHandle, MeshMeta, MeshDef> _meshStore;
    private readonly BackendOps<TextureId, GlTextureHandle, TextureMeta, TextureDef> _textureStore;
    private readonly BackendOps<FrameBufferId, GlFboHandle, FrameBufferMeta, FrameBufferDef> _fboStore;


    internal GlStates(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _meshStore = ctx.Store.VertexArray;
        _textureStore = ctx.Store.Texture;
        _fboStore = ctx.Store.FrameBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearColor(Color4 color) => _gl.ClearColor(color.R, color.G, color.B, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearBuffer(ClearBufferFlag flags) => _gl.Clear(flags.ToGlEnum());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ColorMask(bool v) => _gl.ColorMask(v, v, v, v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TogglePolygonOffset(bool enabled)
    {
        if (enabled) _gl.Enable(GLEnum.PolygonOffsetFill);
        else _gl.Disable(GLEnum.PolygonOffsetFill);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleFrameBufferSrgb(bool enabled)
    {
        if (enabled) _gl.Enable(GLEnum.FramebufferSrgb);
        else _gl.Disable(GLEnum.FramebufferSrgb);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleBlendState(bool enabled)
    {
        if (enabled) _gl.Enable(EnableCap.Blend);
        else _gl.Disable(EnableCap.Blend);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleDepthTest(bool enabled)
    {
        if (enabled) _gl.Enable(EnableCap.DepthTest);
        else _gl.Disable(EnableCap.DepthTest);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleCullFace(bool enabled)
    {
        if (enabled) _gl.Enable(EnableCap.CullFace);
        else _gl.Disable(EnableCap.CullFace);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleDepthMask(bool enabled)
    {
        _gl.DepthMask(enabled);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleScissorTest(bool enabled)
    {
        if (enabled) _gl.Enable(EnableCap.ScissorTest);
        else _gl.Disable(EnableCap.ScissorTest);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetViewport(Bounds2D viewport) =>
        _gl.Viewport(viewport.X, viewport.Y, (uint)viewport.Width, (uint)viewport.Height);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPolygonOffset(float factor, float units) => _gl.PolygonOffset(factor, units);

    public void SetBlendMode(BlendMode blendMode)
    {
        if (blendMode == BlendMode.Unset) return;
        var (eq, src, dst) = blendMode.ToGlEnum();
        _gl.BlendEquation(eq);
        _gl.BlendFunc(src, dst);
    }

    public void SetDepthMode(DepthMode depthMode)
    {
        if (depthMode == DepthMode.Unset) return;
        var func = depthMode.ToGlEnum();
        _gl.DepthFunc(func);
    }

    public void SetCullMode(CullMode cullMode)
    {
        if (cullMode == CullMode.Unset) return;
        var (face, front) = cullMode.ToGlEnum();
        _gl.CullFace(face);
        _gl.FrontFace(front);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindTexture(GfxRefToken<TextureId> texRef, int slot) =>
        _gl.BindTextureUnit((uint)slot, _textureStore.GetHandle(texRef));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindTextureSlot(int slot) => _gl.BindTextureUnit(0, (uint)slot);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindFrameBuffer(GfxRefToken<FrameBufferId> fboRef) =>
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fboStore.GetHandle(fboRef));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindFrameBuffer() => _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindMesh(GfxRefToken<MeshId> mesh) => _gl.BindVertexArray(_meshStore.GetHandle(mesh));

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