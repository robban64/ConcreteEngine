using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
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

    public ResourceRefToken<MeshId> CreateVertexArray()
    {
        _gl.CreateVertexArrays(1, out uint vao);
        return _store.VertexArray.Add(new GlMeshHandle(vao));
    }

    //TODO Binding index?
    public void AttachVertexBuffer(in GfxHandle vao, in GfxHandle vbo, uint bindingIdx, nuint offset, nuint stride)
    {
        var vboHandle = _store.VertexBuffer.Get(in vbo).Handle;
        var handle = VaoHandle(in vao);
        _gl.VertexArrayVertexBuffer(handle, bindingIdx, vboHandle, (nint)offset, (uint)stride);
    }

    public void AttachIndexBuffer(in GfxHandle vao, in GfxHandle ibo)
    {
        var iboHandle = _store.VertexBuffer.Get(in ibo).Handle;
        _gl.VertexArrayElementBuffer(VaoHandle(in vao), iboHandle);
    }

    public unsafe void SetVertexAttribute(in GfxHandle vao, in VertexAttributeDescriptor attr)
    {
        var handle = VaoHandle(vao);
        _gl.VertexArrayAttribFormat(handle, attr.VboBinding, (int)attr.Format, VertexAttribType.Float, attr.Normalized,
            attr.Offset);
        _gl.VertexArrayAttribBinding(handle, attr.VboBinding, attr.VboBinding);
        _gl.EnableVertexArrayAttrib(handle, attr.VboBinding);
        if (attr.Divisor != 0) _gl.VertexArrayBindingDivisor(handle, attr.VboBinding, attr.Divisor);
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