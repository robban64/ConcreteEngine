#region

using ConcreteEngine.Graphics.Error;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public abstract class OpenGLResource : IGraphicsResource
{
    public uint Handle { get; }
    public bool IsDisposed { get; internal set; } = false;

    internal OpenGLResource(uint handle)
    {
        if (handle == 0)
            throw GraphicsException.MissingHandle<OpenGLResource>("this");

        Handle = handle;
    }
}