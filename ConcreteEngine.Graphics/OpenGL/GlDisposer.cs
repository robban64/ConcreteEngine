using ConcreteEngine.Graphics.Gfx.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlDisposer : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly ResourceBackendDispatcher _dispatcher;

    internal GlDisposer(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _dispatcher = ctx.Dispatcher;
    }

    public void DeleteGlResource(in DeleteResourceCommand cmd)
    {
        switch (cmd.Handle.Kind)
        {
            case GraphicsHandleKind.Texture:
                DisposeTexture(in cmd);
                break;
            case GraphicsHandleKind.Shader:
                DisposeShader(in cmd);
                break;
            case GraphicsHandleKind.Mesh:
                DisposeVao(in cmd);
                break;
            case GraphicsHandleKind.VertexBuffer:
                DisposeVbo(in cmd);
                break;
            case GraphicsHandleKind.IndexBuffer:
                DisposeIbo(in cmd);
                break;
            case GraphicsHandleKind.FrameBuffer:
                DisposeFbo(in cmd);
                break;
            case GraphicsHandleKind.RenderBuffer:
                DisposeRbo(in cmd);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(cmd), cmd, $"Invalid resource {cmd.Handle.Kind}");
        }

        _dispatcher.OnDelete(in cmd);
    }

    private void DisposeTexture(in DeleteResourceCommand cmd) => _gl.DeleteTexture(cmd.BackendHandle.Value);

    private void DisposeShader(in DeleteResourceCommand cmd) => _gl.DeleteProgram(cmd.BackendHandle.Value);

    private void DisposeVao(in DeleteResourceCommand cmd) => _gl.DeleteVertexArray(cmd.BackendHandle.Value);

    private void DisposeVbo(in DeleteResourceCommand cmd) => _gl.DeleteBuffer(cmd.BackendHandle.Value);

    private void DisposeIbo(in DeleteResourceCommand cmd) => _gl.DeleteBuffer(cmd.BackendHandle.Value);

    private void DisposeFbo(in DeleteResourceCommand cmd) => _gl.DeleteFramebuffer(cmd.BackendHandle.Value);

    private void DisposeRbo(in DeleteResourceCommand cmd) => _gl.DeleteRenderbuffer(cmd.BackendHandle.Value);
}