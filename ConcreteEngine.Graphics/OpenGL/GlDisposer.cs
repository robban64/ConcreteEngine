#region

using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlDisposer : IGraphicsDriverModule
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

    public void DeleteGfxResource(in DeleteResourceCommand cmd)
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

    private void DisposeTexture(in DeleteResourceCommand cmd)
    {
        _gl.DeleteTexture(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeShader(in DeleteResourceCommand cmd)
    {
        _gl.DeleteProgram(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeVao(in DeleteResourceCommand cmd)
    {
        _gl.DeleteVertexArray(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeVbo(in DeleteResourceCommand cmd)
    {
        _gl.DeleteBuffer(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeIbo(in DeleteResourceCommand cmd)
    {
        _gl.DeleteBuffer(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeFbo(in DeleteResourceCommand cmd)
    {
        _gl.DeleteFramebuffer(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeRbo(in DeleteResourceCommand cmd)
    {
        _gl.DeleteRenderbuffer(cmd.NativeHandle.Value);
        _dispatcher.OnDelete(in cmd);
    }
}