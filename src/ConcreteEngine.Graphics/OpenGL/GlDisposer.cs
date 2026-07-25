using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlDisposer
{
    private static GL Gl => GlBackendDriver.Gl;
    private readonly ResourceBackendDispatcher _dispatcher;

    internal GlDisposer(ResourceBackendDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void DeleteGlResource(DeleteResourceCommand cmd)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cmd.Handle.Value, nameof(cmd.Handle));

        switch (cmd.Kind)
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
            default: throw new ArgumentOutOfRangeException(nameof(cmd));
        }

        _dispatcher.OnDelete(cmd);
    }

    private void DisposeTexture(DeleteResourceCommand cmd) => Gl.DeleteTexture(cmd.Handle);

    private void DisposeShader(DeleteResourceCommand cmd) => Gl.DeleteProgram(cmd.Handle);

    private void DisposeVao(DeleteResourceCommand cmd) => Gl.DeleteVertexArray(cmd.Handle);

    private void DisposeVbo(DeleteResourceCommand cmd) => Gl.DeleteBuffer(cmd.Handle);

    private void DisposeIbo(DeleteResourceCommand cmd) => Gl.DeleteBuffer(cmd.Handle);

    private void DisposeFbo(DeleteResourceCommand cmd) => Gl.DeleteFramebuffer(cmd.Handle);

    private void DisposeRbo(DeleteResourceCommand cmd) => Gl.DeleteRenderbuffer(cmd.Handle);
}