using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx;
using static ConcreteEngine.Graphics.OpenGL.GlDriver;

namespace ConcreteEngine.Graphics.OpenGL;

internal static class GlDisposer
{
    public static void DeleteGlResource(DeleteResourceCommand cmd)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cmd.Handle.Value, nameof(cmd.Handle));

        switch (cmd.Kind)
        {
            case GraphicsKind.Texture: DisposeTexture(cmd); break;
            case GraphicsKind.Shader: DisposeShader(cmd); break;
            case GraphicsKind.Mesh: DisposeVao(cmd); break;
            case GraphicsKind.VertexBuffer: DisposeVbo(cmd); break;
            case GraphicsKind.IndexBuffer: DisposeIbo(cmd); break;
            case GraphicsKind.FrameBuffer: DisposeFbo(cmd); break;
            case GraphicsKind.RenderBuffer: DisposeRbo(cmd); break;
            default: throw new ArgumentOutOfRangeException(nameof(cmd));
        }
        GfxLog.LogBackend(cmd.Handle, cmd.GfxId, cmd.Kind.ToLogTopic(), LogAction.Destroy);
    }

    private static void DisposeTexture(DeleteResourceCommand cmd) => Gl.DeleteTexture(cmd.Handle);

    private static void DisposeShader(DeleteResourceCommand cmd) => Gl.DeleteProgram(cmd.Handle);

    private static void DisposeVao(DeleteResourceCommand cmd) => Gl.DeleteVertexArray(cmd.Handle);

    private static void DisposeVbo(DeleteResourceCommand cmd) => Gl.DeleteBuffer(cmd.Handle);

    private static void DisposeIbo(DeleteResourceCommand cmd) => Gl.DeleteBuffer(cmd.Handle);

    private static void DisposeFbo(DeleteResourceCommand cmd) => Gl.DeleteFramebuffer(cmd.Handle);

    private static void DisposeRbo(DeleteResourceCommand cmd) => Gl.DeleteRenderbuffer(cmd.Handle);
}