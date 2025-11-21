#region

using ConcreteEngine.Graphics.Gfx.Definitions;
using Silk.NET.OpenGL;

#endregion

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