using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlMeshes
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
    public void BindVertexBuffer(in GfxHandle vao, in GfxHandle vbo, uint idx, nint offset, uint stride)
    {
        var vboHandle = _store.VertexBuffer.Get(in vbo).Handle;
        _gl.VertexArrayVertexBuffer(VaoHandle(in vao), idx, vboHandle, offset, stride);
    }

}