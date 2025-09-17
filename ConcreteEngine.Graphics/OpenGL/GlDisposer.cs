using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlDisposer
{
    private readonly GL _gl;
    private readonly BackendOpsHub _store;
    private readonly ResourceBackendDispatcher _dispatcher;

    internal GlDisposer(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _store = ctx.Store;
        _dispatcher = ctx.Dispatcher;
    }

    public void DeleteGfxResource(in DeleteCmd cmd)
    {
        switch (cmd.Handle.Kind)
        {
            case ResourceKind.Texture:
                DisposeTexture(in cmd);
                break;
            case ResourceKind.Shader:
                DisposeShader(in cmd);
                break;
            case ResourceKind.Mesh:
                DisposeVao(in cmd);
                break;
            case ResourceKind.VertexBuffer:
                DisposeVbo(in cmd);
                break;
            case ResourceKind.IndexBuffer:
                DisposeIbo(in cmd);
                break;
            case ResourceKind.FrameBuffer:
                DisposeFbo(in cmd);
                break;
            case ResourceKind.RenderBuffer:
                DisposeRbo(in cmd);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(cmd), cmd, $"Invalid resource {cmd.Handle.Kind}");
        }
    }

    private void DisposeTexture(in DeleteCmd cmd)
    {
        _gl.DeleteTexture(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeShader(in DeleteCmd cmd)
    {
        _gl.DeleteProgram(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeVao(in DeleteCmd cmd)
    {
        _gl.DeleteVertexArray(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeVbo(in DeleteCmd cmd)
    {
        _gl.DeleteBuffer(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeIbo(in DeleteCmd cmd)
    {
        _gl.DeleteBuffer(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeFbo(in DeleteCmd cmd)
    {
        _gl.DeleteFramebuffer(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeRbo(in DeleteCmd cmd)
    {
        _gl.DeleteRenderbuffer(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

}