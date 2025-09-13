using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal abstract class GlFactory()
{
    private GL _gl = null!;
    private DeviceCapabilities _capabilities = null!;

    protected GL Gl => _gl;

    protected DeviceCapabilities Capabilities => _capabilities;

    internal void AttachGlContext(GL gl, DeviceCapabilities capabilities)
    {
        _gl  = gl;
        _capabilities = capabilities;
    }
}