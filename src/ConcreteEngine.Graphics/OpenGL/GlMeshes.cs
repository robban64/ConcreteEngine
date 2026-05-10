using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlMeshes 
{
    private static GL Gl => GlBackendDriver.Gl;
    private readonly BackendResourceStore<GlHandle> _meshStore;
    private readonly BackendResourceStore<GlHandle> _vboStore;
    private readonly BackendResourceStore<GlHandle> _iboStore;

    internal GlMeshes(GlCtx ctx)
    {
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
        Gl.CreateVertexArrays(1, out uint vao);
        return _meshStore.Add(new GlHandle(vao));
    }

    public void AttachIndexBuffer(GfxHandle vao, GfxHandle ibo)
    {
        var iboHandle = _iboStore.GetHandle(ibo).Value;
        Gl.VertexArrayElementBuffer(_meshStore.GetHandle(vao), iboHandle);
    }

    public void AttachVertexBuffer(GfxHandle vao, int binding, GfxHandle vbo,
        in VertexBufferMeta m)
    {
        var vboHandle = _vboStore.GetHandle(vbo);
        var handle = _meshStore.GetHandle(vao);
        Gl.VertexArrayVertexBuffer(handle, (uint)binding, vboHandle, 0, (uint)m.Stride);
        if (m.Divisor != 0)
            Gl.VertexArrayBindingDivisor(handle, (uint)binding, m.Divisor);
    }

/*
    public void AddVertexAttributeRange(GfxHandle vao, IReadOnlyList<VertexAttribute> attribs)
    {
        var vaoHandle = _meshStore.GetHandle(vao);
        for (int i = 0; i < attribs.Count; i++)
            AddVertexAttribute(vaoHandle, attribs[i]);
    }
*/
    public void AddVertexAttributeFromSpan(GfxHandle vao, ReadOnlySpan<VertexAttributeDef> attribs)
    {
        var vaoHandle = _meshStore.GetHandle(vao);
        for (int i = 0; i < attribs.Length; i++)
            AddVertexAttribute(vaoHandle, in attribs[i]);
    }

    private void AddVertexAttribute(GlHandle vao, in VertexAttributeDef a)
    {
        var primitive = a.Format.ToGlEnum();

        switch (a.Format)
        {
            case VertexFormat.Float:
                Gl.VertexArrayAttribFormat(vao, a.Location, a.Components, primitive, a.Normalized, a.Offset);
                break;
            case VertexFormat.Integer:
                Gl.VertexArrayAttribIFormat(vao, a.Location, a.Components, primitive, a.Offset);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(a.Format), a.Format, null);
        }

        Gl.VertexArrayAttribBinding(vao, a.Location, a.Binding);
        Gl.EnableVertexArrayAttrib(vao, a.Location);
    }
}