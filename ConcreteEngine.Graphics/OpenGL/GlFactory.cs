using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal abstract class GlFactory(GL gl, DeviceCapabilities capabilities)
{
    protected GL Gl { get; } = gl;
    protected DeviceCapabilities Capabilities { get; } = capabilities;
}