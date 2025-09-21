using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlStates: IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendOpsHub _store;

    internal GlStates(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _store = ctx.Store;
    }
    
    
    public void Clear(Color4 color, ClearBufferFlag flags)
    {
        _gl.ClearColor(color.R, color.G, color.B, 1);
        _gl.Clear(flags.ToGlEnum());
    }
    
    public void SetViewport(in Vector2D<int> viewport) => _gl.Viewport(viewport);

    public void SetBlendMode(BlendMode blendMode)
    {
        var (enabled, eq, src, dst) = blendMode.ToGlEnum();
        if (enabled)
        {
            _gl.Enable(EnableCap.Blend);
            _gl.BlendEquation(eq);
            _gl.BlendFunc(src, dst);
        }
        else
        {
            _gl.Disable(EnableCap.Blend);
        }
    }

    public void SetDepthMode(DepthMode depthMode)
    {
        var (cap, func, mask) = depthMode.ToGlEnum();
        _gl.Enable(cap);
        _gl.DepthFunc(func);
        _gl.DepthMask(mask);
    }

    public void SetCullMode(CullMode cullMode)
    {
        var (cap, face, front) = cullMode.ToGlEnum();
        _gl.Enable(cap);
        _gl.CullFace(face);
        _gl.FrontFace(front);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindMesh(GfxRefToken<MeshId> mesh) => _gl.BindVertexArray(_store.VertexArray.GetRef(mesh).Handle);
    
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