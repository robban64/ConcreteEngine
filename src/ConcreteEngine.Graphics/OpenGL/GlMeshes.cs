using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlMeshes
{
    private static GL Gl => GlBackendDriver.Gl;
    private readonly BackendResourceStore _meshStore = GfxRegistry.GetBackendStore<MeshMeta>();
    private readonly BackendResourceStore _vboStore = GfxRegistry.GetBackendStore<VertexBufferMeta>();
    private readonly BackendResourceStore _iboStore = GfxRegistry.GetBackendStore<IndexBufferMeta>();


    public void EnsureCapacity(int capacity)
    {
        _meshStore.EnsureCapacity(capacity);
        _vboStore.EnsureCapacity(capacity);
        _iboStore.EnsureCapacity(capacity);
    }

    public GfxHandle CreateVertexArray()
    {
        Gl.CreateVertexArrays(1, out uint vao);
        return _meshStore.Add(new NativeHandle(vao));
    }

    public void AttachIndexBuffer(GfxHandle vao, GfxHandle ibo)
    {
        var iboHandle = _iboStore.Get(ibo);
        Gl.VertexArrayElementBuffer(_meshStore.Get(vao), iboHandle);
    }

    public void AttachVertexBuffer(GfxHandle vao, int binding, GfxHandle vbo,
        in VertexBufferMeta m)
    {
        var vboHandle = _vboStore.Get(vbo);
        var handle = _meshStore.Get(vao);
        Gl.VertexArrayVertexBuffer(handle, (uint)binding, vboHandle, 0, (uint)m.Stride);
        if (m.Divisor != 0)
            Gl.VertexArrayBindingDivisor(handle, (uint)binding, m.Divisor);
    }

    public void AddVertexAttributes(GfxHandle vao, ReadOnlySpan<VertexAttributeDef> attribs)
    {
        var vaoHandle = _meshStore.Get(vao);
        foreach (var attrib in attribs)
            AddVertexAttribute(vaoHandle, attrib);
    }

    private static void AddVertexAttribute(NativeHandle vao, VertexAttributeDef a)
    {
        var primitive = a.Format.ToGlEnum();

        switch (a.Format)
        {
            case VertexFormat.Int:
            case VertexFormat.UInt:
                Gl.VertexArrayAttribIFormat(vao, a.Location, a.Components, primitive, a.Offset);
                break;
            case VertexFormat.Float:
            case VertexFormat.Half:
                Gl.VertexArrayAttribFormat(vao, a.Location, a.Components, primitive, a.Normalized, a.Offset);
                break;
            case VertexFormat.UByte:
                if (a.Normalized)
                    Gl.VertexArrayAttribFormat(vao, a.Location, a.Components, primitive, a.Normalized, a.Offset);
                else
                    Gl.VertexArrayAttribIFormat(vao, a.Location, a.Components, primitive, a.Offset);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(a.Format), a.Format, null);
        }

        Gl.VertexArrayAttribBinding(vao, a.Location, a.Binding);
        Gl.EnableVertexArrayAttrib(vao, a.Location);
    }
}