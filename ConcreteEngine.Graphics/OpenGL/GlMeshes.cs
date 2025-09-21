using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlMeshes : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendOpsHub _store;
    private readonly GlCapabilities _capabilities;

    internal GlMeshes(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _capabilities = ctx.Capabilities;
        _store = ctx.Store;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint VaoHandle(in GfxRefToken<MeshId> vao) => _store.VertexArray.GetRef(in vao).Handle;

    public GfxRefToken<MeshId> CreateVertexArray()
    {
        _gl.CreateVertexArrays(1, out uint vao);
        return _store.VertexArray.Add(new GlMeshHandle(vao));
    }

    //TODO Binding index?
    public void AttachVertexBuffer(in GfxRefToken<MeshId> vao, in GfxRefToken<VertexBufferId> vbo, int bindingIdx,
        nint offset, nint stride)
    {
        var vboHandle = _store.VertexBuffer.GetRef(in vbo).Handle;
        var handle = VaoHandle(in vao);
        _gl.VertexArrayVertexBuffer(handle, (uint)bindingIdx, vboHandle, offset, (uint)stride);
    }

    public void AttachIndexBuffer(in GfxRefToken<MeshId> vao, in GfxRefToken<IndexBufferId> ibo)
    {
        var iboHandle = _store.IndexBuffer.GetRef(in ibo).Handle;
        _gl.VertexArrayElementBuffer(VaoHandle(in vao), iboHandle);
    }

    public void AddVertexAttribute(GfxRefToken<MeshId> vao, int attribIndex, in VertexAttributeDesc attr) =>
        AddVertexAttributeInternal(VaoHandle(vao), attribIndex, attr);

    public void AddVertexAttributeRange(GfxRefToken<MeshId> vao, IReadOnlyList<VertexAttributeDesc> attribs)
    {
        var vaoHandle = VaoHandle(vao);
        for (int i = 0; i < attribs.Count; i++)
            AddVertexAttributeInternal(vaoHandle, i, attribs[i]);
    }

    public void AddVertexAttributeFromSpan(GfxRefToken<MeshId> vao, ReadOnlySpan<VertexAttributeDesc> attribs)
    {
        var vaoHandle = VaoHandle(vao);
        for (int i = 0; i < attribs.Length; i++)
            AddVertexAttributeInternal(vaoHandle, i, attribs[i]);
    }



    //for (int i = 0; i < attribs.Length; i++)
    private void AddVertexAttributeInternal(uint vaoHandle, int attribIndex, in VertexAttributeDesc attr)
    {
        (uint vboIdx, int format, uint divisor) = ((uint)attr.VboBinding, (int)attr.Format, (uint)attr.Divisor);
        _gl.VertexArrayAttribFormat(vaoHandle, vboIdx, format,
            VertexAttribType.Float, attr.Normalized, (uint)attr.Offset);

        _gl.VertexArrayAttribBinding(vaoHandle, (uint)attribIndex, vboIdx);
        _gl.EnableVertexArrayAttrib(vaoHandle, vboIdx);

        if (divisor != 0) _gl.VertexArrayBindingDivisor(vaoHandle, vboIdx, divisor);
    }


}