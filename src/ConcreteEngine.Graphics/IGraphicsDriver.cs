using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.OpenGL;

namespace ConcreteEngine.Graphics;

internal interface IGraphicsDriverModule;

internal interface IGraphicsDriver
{
    DeviceCapabilities Capabilities { get; }
    void EndFrame();

    IDriverDebugger Debugger { get; }
    GlDisposer Disposer { get; }
    GlBuffers Buffers { get; }
    GlTextures Textures { get; }
    GlMeshes Meshes { get; }
    GlShaders Shaders { get; }
    GlStates States { get; }
    GlFrameBuffers FrameBuffers { get; }
}