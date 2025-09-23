#region

using ConcreteEngine.Graphics.OpenGL;

#endregion

namespace ConcreteEngine.Graphics;

internal interface IGraphicsDriverModule;

internal interface IGraphicsDriver
{
    GraphicsConfiguration Configuration { get; }
    DeviceCapabilities Capabilities { get; }
    void EndFrame();

    GlDebugger Debugger { get; }
    GlDisposer Disposer { get; }
    GlBuffers Buffers { get; }
    GlTextures Textures { get; }
    GlMeshes Meshes { get; }
    GlShaders Shaders { get; }
    GlStates States { get; }
    GlFrameBuffers FrameBuffers { get; }
}