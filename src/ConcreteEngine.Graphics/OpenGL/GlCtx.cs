using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlCtx
{
    public required GL Gl { get; init; }
    public required BackendStoreHub Store { get; init; }
    public required GlCapabilities Capabilities { get; init; }
    public required ResourceBackendDispatcher Dispatcher { get; init; }
}