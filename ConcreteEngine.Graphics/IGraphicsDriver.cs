using ConcreteEngine.Common;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

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