using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlCapabilities: IGraphicsDriverModule
{
    private DeviceCapabilities _capabilities;

    internal GlCapabilities()
    {
    }

    public DeviceCapabilities Caps => _capabilities;

    public DeviceCapabilities CreateDeviceCapabilities(GL gl)
    {
        if (_capabilities != null) throw new InvalidOperationException("Capabilities already created.");

        gl.GetInteger(GetPName.MajorVersion, out int glMajor);
        gl.GetInteger(GetPName.MinorVersion, out int glMinor);

        return _capabilities = new DeviceCapabilities
        {
            GlVersion = new OpenGlVersion(glMajor, glMinor),
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
            UniformBufferOffsetAlignment = gl.GetInteger(GetPName.UniformBufferOffsetAlignment),
        };
    }
}