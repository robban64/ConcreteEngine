using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Data;
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

    public void DeleteGlResource(DeleteResourceCommand cmd)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cmd.BackendHandle.Value,nameof(cmd.BackendHandle));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)cmd.BackendHandle.Value, uint.MaxValue);

        switch (cmd.Handle.Kind)
        {
            case GraphicsKind.Texture:
                DisposeTexture(cmd);
                break;
            case GraphicsKind.Shader:
                DisposeShader(cmd);
                break;
            case GraphicsKind.Mesh:
                DisposeVao(cmd);
                break;
            case GraphicsKind.VertexBuffer:
                DisposeVbo(cmd);
                break;
            case GraphicsKind.IndexBuffer:
                DisposeIbo(cmd);
                break;
            case GraphicsKind.FrameBuffer:
                DisposeFbo(cmd);
                break;
            case GraphicsKind.RenderBuffer:
                DisposeRbo(cmd);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(cmd), cmd, $"Invalid resource {cmd.Handle.Kind}");
        }

        _dispatcher.OnDelete(cmd);
    }

    private void DisposeTexture(DeleteResourceCommand cmd) => _gl.DeleteTexture(cmd.BackendHandle);

    private void DisposeShader(DeleteResourceCommand cmd) => _gl.DeleteProgram(cmd.BackendHandle);

    private void DisposeVao(DeleteResourceCommand cmd) => _gl.DeleteVertexArray(cmd.BackendHandle);

    private void DisposeVbo(DeleteResourceCommand cmd) => _gl.DeleteBuffer(cmd.BackendHandle);

    private void DisposeIbo(DeleteResourceCommand cmd) => _gl.DeleteBuffer(cmd.BackendHandle);

    private void DisposeFbo(DeleteResourceCommand cmd) => _gl.DeleteFramebuffer(cmd.BackendHandle);

    private void DisposeRbo(DeleteResourceCommand cmd) => _gl.DeleteRenderbuffer(cmd.BackendHandle);
}