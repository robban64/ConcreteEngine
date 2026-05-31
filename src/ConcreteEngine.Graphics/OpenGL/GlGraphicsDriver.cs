using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlBackendDriver
{
    internal static GL Gl = null!;

    public GlStates States { get; }
    public GlBuffers Buffers { get; }
    public GlTextures Textures { get; }
    public GlMeshes Meshes { get; }
    public GlShaders Shaders { get; }
    public GlFrameBuffers FrameBuffers { get; }
    public GlDebugger Debugger { get; }
    public GlDisposer Disposer { get; }

    public readonly GlCapabilities Capabilities;


    internal GlBackendDriver(GlStartupConfig config, GfxResourceManager resource)
    {
        Gl = config.DriverContext;
        Capabilities = new GlCapabilities();

        Debugger = new GlDebugger();
        Disposer = new GlDisposer(resource.BackendDispatcher);
        Buffers = new GlBuffers();
        Textures = new GlTextures();
        Meshes = new GlMeshes();
        Shaders = new GlShaders();
        States = new GlStates();
        FrameBuffers = new GlFrameBuffers();
    }


    internal GlCapabilities Initialize()
    {
        Capabilities.CreateDeviceCapabilities(Gl);
        Debugger.EnableGlDebug();

        Gl.Enable(GLEnum.Dither);
        Gl.Enable(GLEnum.Multisample);
        Gl.Enable(EnableCap.TextureCubeMapSeamless);
        Gl.PixelStore(GLEnum.UnpackAlignment, 1);

        Gl.DepthMask(true);

        Gl.Enable(EnableCap.CullFace);
        Gl.CullFace(TriangleFace.Back);
        Gl.FrontFace(FrontFaceDirection.Ccw);

        return Capabilities;
    }
}