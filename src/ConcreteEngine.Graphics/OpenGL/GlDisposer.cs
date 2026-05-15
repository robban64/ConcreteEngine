using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlDisposer
{
    private static GL Gl => GlBackendDriver.Gl;
    private readonly ResourceBackendDispatcher _dispatcher;

    internal GlDisposer(GlCtx ctx)
    {
        _dispatcher = ctx.Dispatcher;
    }

    public void DeleteGlResource(DeleteResourceCommand cmd)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cmd.BackendHandle.Value, nameof(cmd.BackendHandle));
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

    private void DisposeTexture(DeleteResourceCommand cmd) => Gl.DeleteTexture(cmd.BackendHandle);

    private void DisposeShader(DeleteResourceCommand cmd) => Gl.DeleteProgram(cmd.BackendHandle);

    private void DisposeVao(DeleteResourceCommand cmd) => Gl.DeleteVertexArray(cmd.BackendHandle);

    private void DisposeVbo(DeleteResourceCommand cmd) => Gl.DeleteBuffer(cmd.BackendHandle);

    private void DisposeIbo(DeleteResourceCommand cmd) => Gl.DeleteBuffer(cmd.BackendHandle);

    private void DisposeFbo(DeleteResourceCommand cmd) => Gl.DeleteFramebuffer(cmd.BackendHandle);

    private void DisposeRbo(DeleteResourceCommand cmd) => Gl.DeleteRenderbuffer(cmd.BackendHandle);
}