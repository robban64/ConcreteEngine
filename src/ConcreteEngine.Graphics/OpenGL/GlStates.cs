using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;
using static ConcreteEngine.Graphics.OpenGL.GlDriver;

namespace ConcreteEngine.Graphics.OpenGL;

internal static class GlStates
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearColor(ColorRgba color)
    {
        var c = (Color4)color;
        Gl.ClearColor(c.R, c.G, c.B, c.A);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearBuffer(ClearBufferFlag flags) => Gl.Clear(flags.ToGlEnum());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ColorMask(bool v) => Gl.ColorMask(v, v, v, v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToggleDepthMask(bool enabled) => Gl.DepthMask(enabled);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToggleStateFlag(GfxStateFlags flag, bool enabled)
    {
        if (flag == GfxStateFlags.DepthWrite)
            Gl.DepthMask(enabled);
        else if (flag == GfxStateFlags.ColorMask)
            Gl.ColorMask(enabled, enabled, enabled, enabled);
        else
        {
            var enableCap = flag.ToGlEnableCap();
            if (enabled) Gl.Enable(enableCap);
            else Gl.Disable(enableCap);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToggleSampleAlphaCoverage(bool enabled)
    {
        if (enabled) Gl.Enable(EnableCap.SampleAlphaToCoverage);
        else Gl.Disable(EnableCap.SampleAlphaToCoverage);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TogglePolygonOffset(bool enabled)
    {
        if (enabled) Gl.Enable(EnableCap.PolygonOffsetFill);
        else Gl.Disable(EnableCap.PolygonOffsetFill);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToggleFrameBufferSrgb(bool enabled)
    {
        if (enabled) Gl.Enable(EnableCap.FramebufferSrgb);
        else Gl.Disable(EnableCap.FramebufferSrgb);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToggleBlendState(bool enabled)
    {
        if (enabled) Gl.Enable(EnableCap.Blend);
        else Gl.Disable(EnableCap.Blend);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToggleDepthTest(bool enabled)
    {
        if (enabled) Gl.Enable(EnableCap.DepthTest);
        else Gl.Disable(EnableCap.DepthTest);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToggleCullFace(bool enabled)
    {
        if (enabled) Gl.Enable(EnableCap.CullFace);
        else Gl.Disable(EnableCap.CullFace);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToggleScissorTest(bool enabled)
    {
        if (enabled) Gl.Enable(EnableCap.ScissorTest);
        else Gl.Disable(EnableCap.ScissorTest);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetViewport(Size2D vp) => Gl.Viewport(0, 0, (uint)vp.Width, (uint)vp.Height);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetPolygonOffset(float factor, float units) => Gl.PolygonOffset(factor, units);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBlendMode(BlendMode blendMode)
    {
        if (blendMode == BlendMode.Unset) return;
        blendMode.ToGlEnum(out var src, out var dst);
        Gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
        Gl.BlendFunc(src, dst);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetDepthMode(DepthMode depthMode)
    {
        if (depthMode == DepthMode.Unset) return;
        Gl.DepthFunc(depthMode.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetCullMode(CullMode cullMode)
    {
        if (cullMode == CullMode.Unset) return;
        var (face, front) = cullMode.ToGlEnum();
        Gl.CullFace(face);
        Gl.FrontFace(front);
    }

    public static void BindAllTextures(ReadOnlySpan<uint> textures)
    {
        Gl.BindTextures(0, textures);
    }

    public static void UnbindAllTextures()
    {
        Gl.BindTextures(0, stackalloc uint[GfxLimits.TextureSlots]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BindTexture(GfxHandle textureHandle, int slot) => Gl.BindTextureUnit((uint)slot, textureHandle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnbindTextureSlot(int slot) => Gl.BindTextureUnit((uint)slot, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BindFrameBuffer(GfxHandle fboHandle) =>
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnbindFrameBuffer() => Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BindMesh(GfxHandle meshHandle) => Gl.BindVertexArray(meshHandle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnbindMesh() => Gl.BindVertexArray(0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UseShader(GfxHandle shanderHandle) => Gl.UseProgram(shanderHandle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnbindShader() => Gl.UseProgram(0);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Draw(in MeshMeta meta)
    {
        var glPrimitive = meta.Primitive.ToGlEnum();
        if(meta.ElementSize != DrawElementSize.None)
            Gl.DrawElements(glPrimitive, meta.DrawCount, meta.ElementSize.ToGlEnum(), (void*)0);
        else
            Gl.DrawArrays(glPrimitive, 0, meta.DrawCount);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void DrawInstance(in MeshMeta meta, uint instances)
    {
        var glPrimitive = meta.Primitive.ToGlEnum();
        if(meta.ElementSize != DrawElementSize.None)
            Gl.DrawElementsInstanced(glPrimitive, meta.DrawCount, meta.ElementSize.ToGlEnum(), (void*)0, instances);
        else
            Gl.DrawArraysInstanced(glPrimitive, 0, meta.DrawCount, instances);
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawArrays(DrawPrimitive primitive, uint count)
    {
        Gl.DrawArrays(primitive.ToGlEnum(), 0, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void DrawElements(DrawPrimitive primitive, DrawElementSize elementSize, uint count)
    {
        Gl.DrawElements(primitive.ToGlEnum(), count, elementSize.ToGlEnum(), (void*)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawInstanced(DrawPrimitive primitive, uint count, uint instances)
    {
        Gl.DrawArraysInstanced(primitive.ToGlEnum(), 0, count, instances);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void DrawElementsInstanced(DrawPrimitive primitive, DrawElementSize size, uint count, uint instances)
    {
        Gl.DrawElementsInstanced(primitive.ToGlEnum(), count, size.ToGlEnum(), (void*)0, instances);
    }
}