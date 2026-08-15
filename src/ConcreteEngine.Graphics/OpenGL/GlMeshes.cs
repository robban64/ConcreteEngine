using ConcreteEngine.Graphics.Gfx;
using static ConcreteEngine.Graphics.OpenGL.GlDriver;

namespace ConcreteEngine.Graphics.OpenGL;

internal static class GlMeshes
{
    public static NativeHandle<MeshMeta> CreateVertexArray()
    {
        Gl.CreateVertexArrays(1, out uint vao);
        return new NativeHandle<MeshMeta>(vao);
    }

    public static void AttachIndexBuffer(NativeHandle<MeshMeta> vao, NativeHandle<IndexBufferMeta> ibo)
    {
        Gl.VertexArrayElementBuffer(vao, ibo);
    }

    public static void AttachVertexBuffer(NativeHandle<MeshMeta> vao, int binding, NativeHandle<VertexBufferMeta> vbo,
        in VertexBufferMeta meta)
    {
        Gl.VertexArrayVertexBuffer(vao, (uint)binding, vbo, 0, (uint)meta.Stride);
        if (meta.Divisor != 0)
            Gl.VertexArrayBindingDivisor(vao, (uint)binding, meta.Divisor);
    }

    public static void AddVertexAttributes(NativeHandle<MeshMeta> vao, ReadOnlySpan<VertexAttributeDef> attribs)
    {
        foreach (var attrib in attribs)
            AddVertexAttribute(vao, attrib);
    }

    private static void AddVertexAttribute(NativeHandle<MeshMeta> vao, VertexAttributeDef a)
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