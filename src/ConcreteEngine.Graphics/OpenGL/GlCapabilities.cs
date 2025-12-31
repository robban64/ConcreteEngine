using ConcreteEngine.Graphics.Configuration;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlCapabilities
{
    private GpuDeviceCapabilities _capabilities;
    private bool _hasInitialized;

    public OpenGlVersion GlVersion { get; private set; }

    public ref readonly GpuDeviceCapabilities Capabilities => ref _capabilities;

    internal GlCapabilities()
    {
    }

    public void CreateDeviceCapabilities(GL gl)
    {
        if (_hasInitialized) throw new InvalidOperationException("Capabilities already created.");

        _hasInitialized = true;

        gl.GetInteger(GetPName.MajorVersion, out int glMajor);
        gl.GetInteger(GetPName.MinorVersion, out int glMinor);

        GlVersion = new OpenGlVersion(glMajor, glMinor);

        _capabilities = new GpuDeviceCapabilities
        {
            MaxTextureImageUnits = gl.GetInteger(GLEnum.MaxCombinedTextureImageUnits),
            MaxVertexAttribBindings = gl.GetInteger((GLEnum)0x82DA), // GL_MAX_VERTEX_ATTRIB_BINDINGS
            MaxTextureSize = gl.GetInteger(GLEnum.MaxTextureSize),
            MaxArrayTextureLayers = gl.GetInteger(GLEnum.MaxArrayTextureLayers),
            MaxFramebufferWidth = gl.GetInteger((GLEnum)0x9315), // GL_MAX_FRAMEBUFFER_WIDTH
            MaxFramebufferHeight = gl.GetInteger((GLEnum)0x9316), // GL_MAX_FRAMEBUFFER_HEIGHT
            MaxSamples = gl.GetInteger(GLEnum.MaxSamples),
            MaxColorAttachments = gl.GetInteger(GLEnum.MaxColorAttachments),
            MaxAnisotropy = gl.GetFloat(GLEnum.MaxTextureMaxAnisotropy),
            MaxUniformBlockSize = gl.GetInteger(GLEnum.MaxUniformBlockSize),
            UniformBufferOffsetAlignment = gl.GetInteger(GetPName.UniformBufferOffsetAlignment)
        };
    }
}