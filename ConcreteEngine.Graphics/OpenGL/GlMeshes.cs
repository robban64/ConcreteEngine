using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlMeshes: IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendOpsHub _store;
    private readonly GlCapabilities _capabilities;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint VaoHandle(in GfxHandle handle) => _store.VertexArray.Get(in handle).Handle;

    internal GlMeshes(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _capabilities = ctx.Capabilities;
        _store = ctx.Store;
    }

    public GfxRefToken<MeshId> CreateVertexArray()
    {
        _gl.CreateVertexArrays(1, out uint vao);
        return _store.VertexArray.Add(new GlMeshHandle(vao));
    }

    //TODO Binding index?
    public void AttachVertexBuffer(in GfxHandle vao, in GfxHandle vbo, int bindingIdx, nint offset, nint stride)
    {
        var vboHandle = _store.VertexBuffer.Get(in vbo).Handle;
        var handle = VaoHandle(in vao);
        _gl.VertexArrayVertexBuffer(handle, (uint)bindingIdx, vboHandle, offset, (uint)stride);
    }

    public void AttachIndexBuffer(in GfxHandle vao, in GfxHandle ibo)
    {
        var iboHandle = _store.VertexBuffer.Get(in ibo).Handle;
        _gl.VertexArrayElementBuffer(VaoHandle(in vao), iboHandle);
    }

    public void SetVertexAttribute(GfxRefToken<MeshId> vao, int attribIndex, in VertexAttributeDesc attr)
    {
        var handle = VaoHandle(vao.Handle);
        (uint vboIdx, int format, uint divisor) = ((uint)attr.VboBinding,  (int)attr.Format, (uint)attr.Divisor);
        
        _gl.VertexArrayAttribFormat(handle, vboIdx, format, 
            VertexAttribType.Float, attr.Normalized, (uint)attr.Offset);
        
        _gl.VertexArrayAttribBinding(handle, (uint)attribIndex,vboIdx);
        _gl.EnableVertexArrayAttrib(handle, vboIdx);
        
        if (attr.Divisor != 0) _gl.VertexArrayBindingDivisor(handle, vboIdx, (uint)attr.Divisor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawArrays(DrawPrimitive primitive, uint drawCount)
    {
        _gl.DrawArrays(primitive.ToGlEnum(), 0, drawCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void DrawElements(DrawPrimitive primitive, DrawElementSize elementSize, uint drawCount)
    {
        _gl.DrawElements(primitive.ToGlEnum(), drawCount, elementSize.ToGlEnum(), (void*)0);
    }

}