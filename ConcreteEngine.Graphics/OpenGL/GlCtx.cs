#region

using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlCtx
{
    public required GL Gl { get; init; }
    public required BackendOpsHub Store { get; init; }
    public required GlCapabilities Capabilities { get; init; }
    public required ResourceBackendDispatcher Dispatcher { get; init; }
}