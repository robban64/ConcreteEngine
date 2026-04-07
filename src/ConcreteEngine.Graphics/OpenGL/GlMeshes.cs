using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlMeshes : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendResourceStore<GlMeshHandle> _meshStore;
    private readonly BackendResourceStore<GlVboHandle> _vboStore;
    private readonly BackendResourceStore<GlIboHandle> _iboStore;

    internal GlMeshes(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _meshStore = ctx.Store.MeshStore;
        _vboStore = ctx.Store.VboStore;
        _iboStore = ctx.Store.IboStore;
    }

    public void EnsureCapacity(int capacity)
    {
        _meshStore.EnsureCapacity(capacity);
        _vboStore.EnsureCapacity(capacity);
        _iboStore.EnsureCapacity(capacity);
    }

    public GfxHandle CreateVertexArray()
    {
        _gl.CreateVertexArrays(1, out uint vao);
        return _meshStore.Add(new GlMeshHandle(vao));
    }

    public void AttachIndexBuffer(GfxHandle vao, GfxHandle ibo)
    {
        var iboHandle = _iboStore.GetHandle(ibo).Value;
        _gl.VertexArrayElementBuffer(_meshStore.GetHandle(vao), iboHandle);
    }

    public void AttachVertexBuffer(GfxHandle vao, int binding, GfxHandle vbo,
        in VertexBufferMeta m)
    {
        var vboHandle = _vboStore.GetHandle(vbo);
        var handle = _meshStore.GetHandle(vao);
        _gl.VertexArrayVertexBuffer(handle, (uint)binding, vboHandle, 0, (uint)m.Stride);
        if (m.Divisor != 0)
            _gl.VertexArrayBindingDivisor(handle, (uint)binding, m.Divisor);
    }

/*
    public void AddVertexAttributeRange(GfxHandle vao, IReadOnlyList<VertexAttribute> attribs)
    {
        var vaoHandle = _meshStore.GetHandle(vao);
        for (int i = 0; i < attribs.Count; i++)
            AddVertexAttribute(vaoHandle, attribs[i]);
    }
*/
    public void AddVertexAttributeFromSpan(GfxHandle vao, ReadOnlySpan<VertexAttribute> attribs)
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