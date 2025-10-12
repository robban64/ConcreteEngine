#region

using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using Silk.NET.OpenGL;

#endregion


namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlMeshes : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendOpsHub _store;
    private readonly GlCapabilities _capabilities;
    private readonly BackendOps<MeshId, GlMeshHandle, MeshMeta, MeshDef> _meshStore;

    internal GlMeshes(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _capabilities = ctx.Capabilities;
        _store = ctx.Store;
        _meshStore = _store.VertexArray;
    }

    public GfxRefToken<MeshId> CreateVertexArray()
    {
        _gl.CreateVertexArrays(1, out uint vao);
        return _store.VertexArray.Add(new GlMeshHandle(vao));
    }

    //TODO Binding index?
    public void AttachVertexBuffer(in GfxRefToken<MeshId> vao, in GfxRefToken<VertexBufferId> vbo, int bindingIdx,
        nint offset, nint stride)
    {
        var vboHandle = _store.VertexBuffer.GetHandle(vbo);
        var handle = _meshStore.GetHandle(vao);
        _gl.VertexArrayVertexBuffer(handle, (uint)bindingIdx, vboHandle, offset, (uint)stride);
    }

    public void AttachIndexBuffer(in GfxRefToken<MeshId> vao, in GfxRefToken<IndexBufferId> ibo)
    {
        var iboHandle = _store.IndexBuffer.GetHandle(ibo).Value;
        _gl.VertexArrayElementBuffer(_meshStore.GetHandle(vao), iboHandle);
    }

    public void AddVertexAttributeRange(GfxRefToken<MeshId> vao, IReadOnlyList<VertexAttributeDesc> attribs)
    {
        var vaoHandle = _meshStore.GetHandle(vao);
        for (int i = 0; i < attribs.Count; i++)
            AddVertexAttributeInternal(vaoHandle, (uint)i, attribs[i]);
    }

    public void AddVertexAttributeFromSpan(GfxRefToken<MeshId> vao, ReadOnlySpan<VertexAttributeDesc> attribs)
    {
        var vaoHandle = _meshStore.GetHandle(vao);
        for (int i = 0; i < attribs.Length; i++)
            AddVertexAttributeInternal(vaoHandle, (uint)i, attribs[i]);
    }


    private void AddVertexAttributeInternal(uint vao, uint attribIdx, in VertexAttributeDesc attr)
    {
        var (vboIdx, offset) = ((uint)attr.VboBinding, (uint)attr.Offset);
        var size = attr.Components;
        var primitive = VertexAttribType.Float;

        _gl.VertexArrayAttribFormat(vao, attribIdx, size, primitive, attr.Norm, offset);

        _gl.VertexArrayAttribBinding(vao, attribIdx, vboIdx);
        _gl.EnableVertexArrayAttrib(vao, attribIdx);

        //if (divisor != 0) _gl.VertexArrayBindingDivisor(vao, vboIdx, divisor);
    }
}