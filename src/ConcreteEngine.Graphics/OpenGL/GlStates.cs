using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlStates
{
    private readonly GL _gl = GlBackendDriver.Gl;
    private readonly BackendResourceStore _meshStore = GfxRegistry.GetBackendStore<MeshMeta>();
    private readonly BackendResourceStore _textureStore = GfxRegistry.GetBackendStore<TextureMeta>();
    private readonly BackendResourceStore _fboStore = GfxRegistry.GetBackendStore<FrameBufferMeta>();
    private readonly BackendResourceStore _shaderStore = GfxRegistry.GetBackendStore<ShaderMeta>();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearColor(ColorRgba color)
    {
        var c = (Color4)color;
        _gl.ClearColor(c.R, c.G, c.B, c.A);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearBuffer(ClearBufferFlag flags) => _gl.Clear(flags.ToGlEnum());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ColorMask(bool v) => _gl.ColorMask(v, v, v, v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleSampleAlphaCoverage(bool enabled)
    {
        if (enabled) _gl.Enable(GLEnum.SampleAlphaToCoverage);
        else _gl.Disable(GLEnum.SampleAlphaToCoverage);
    }

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
    public void ToggleDepthMask(bool enabled) => _gl.DepthMask(enabled);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleScissorTest(bool enabled)
    {
        if (enabled) _gl.Enable(EnableCap.ScissorTest);
        else _gl.Disable(EnableCap.ScissorTest);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetViewport(Size2D vp) => _gl.Viewport(0, 0, (uint)vp.Width, (uint)vp.Height);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPolygonOffset(float factor, float units) => _gl.PolygonOffset(factor, units);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlendMode(BlendMode blendMode)
    {
        if (blendMode == BlendMode.Unset) return;
        blendMode.ToGlEnum(out var src, out var dst);
        _gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
        _gl.BlendFunc(src, dst);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDepthMode(DepthMode depthMode)
    {
        if (depthMode == DepthMode.Unset) return;
        _gl.DepthFunc(depthMode.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCullMode(CullMode cullMode)
    {
        if (cullMode == CullMode.Unset) return;
        var (face, front) = cullMode.ToGlEnum();
        _gl.CullFace(face);
        _gl.FrontFace(front);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindAllTextures()
    {
        for (uint i = 0; i < GfxLimits.TextureSlots; i++)
            _gl.BindTextureUnit(i, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindTexture(GfxHandle texRef, int slot) =>
        _gl.BindTextureUnit((uint)slot, _textureStore.Get(texRef));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindTextureSlot(int slot) => _gl.BindTextureUnit((uint)slot, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindFrameBuffer(GfxHandle fboRef) =>
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fboStore.Get(fboRef));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindFrameBuffer() => _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindMesh(GfxHandle mesh) => _gl.BindVertexArray(_meshStore.Get(mesh));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindMesh() => _gl.BindVertexArray(0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UseShader(GfxHandle shaderRef) => _gl.UseProgram(_shaderStore.Get(shaderRef));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindShader() => _gl.UseProgram(0);
}