#region

using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using Silk.NET.OpenGL;

#endregion


namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlMeshes : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly GlCapabilities _capabilities;
    private readonly BackendResourceStore<MeshId, GlMeshHandle> _meshStore;
    private readonly BackendResourceStore<VertexBufferId, GlVboHandle> _vboStore;
    private readonly BackendResourceStore<IndexBufferId, GlIboHandle> _iboStore;

    internal GlMeshes(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _capabilities = ctx.Capabilities;
        _meshStore = ctx.Store.VertexArray;
        _vboStore = ctx.Store.VertexBuffer;
        _iboStore = ctx.Store.IndexBuffer;
    }

    public GfxRefToken<MeshId> CreateVertexArray()
    {
        _gl.CreateVertexArrays(1, out uint vao);
        return _meshStore.Add(new GlMeshHandle(vao));
    }

    public void AttachIndexBuffer(GfxRefToken<MeshId> vao, GfxRefToken<IndexBufferId> ibo)
    {
        var iboHandle = _iboStore.GetHandle(ibo).Value;
        _gl.VertexArrayElementBuffer(_meshStore.GetHandle(vao), iboHandle);
    }

    public void AttachVertexBuffer(GfxRefToken<MeshId> vao, int binding, GfxRefToken<VertexBufferId> vbo,
        in VertexBufferMeta m)
    {
        var vboHandle = _vboStore.GetHandle(vbo);
        var handle = _meshStore.GetHandle(vao);
        _gl.VertexArrayVertexBuffer(handle, (uint)binding, vboHandle, 0, (uint)m.Stride);
        if (m.Divisor != 0)
            _gl.VertexArrayBindingDivisor(handle, (uint)binding, m.Divisor);
    }

    public void AddVertexAttributeRange(GfxRefToken<MeshId> vao, IReadOnlyList<VertexAttribute> attribs)
    {
        var vaoHandle = _meshStore.GetHandle(vao);
        for (int i = 0; i < attribs.Count; i++)
            AddVertexAttribute(vaoHandle, attribs[i]);
    }

    public void AddVertexAttributeFromSpan(GfxRefToken<MeshId> vao, ReadOnlySpan<VertexAttribute> attribs)
    {
        var vaoHandle = _meshStore.GetHandle(vao);
        for (int i = 0; i < attribs.Length; i++)
            AddVertexAttribute(vaoHandle, in attribs[i]);
    }

    private void AddVertexAttribute(GlMeshHandle vao, in VertexAttribute a)
    {
        var primitive = a.Format.ToGlEnum();

        switch (a.Format)
        {
            case VertexFormat.Float:
                _gl.VertexArrayAttribFormat(vao, a.Location, a.Components, primitive, a.Normalized, (uint)a.Offset);
                break;
            case VertexFormat.Integer:
                _gl.VertexArrayAttribIFormat(vao, a.Location, a.Components, primitive, (uint)a.Offset);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(a.Format), a.Format, null);
        }

        _gl.VertexArrayAttribBinding(vao, a.Location, a.Binding);
        _gl.EnableVertexArrayAttrib(vao, a.Location);
    }
}