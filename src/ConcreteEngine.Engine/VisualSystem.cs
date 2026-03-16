using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Configuration;

namespace ConcreteEngine.Engine;

internal sealed class VisualSystem
{
    internal static readonly VisualSystem Instance = new();

    public readonly VisualEnvironment VisualEnv;

    private VisualSystem()
    {
        var shadowSize = EngineSettings.Instance.Graphics.ShadowSize;
        var windowSize = EngineSettings.Instance.Display.WindowSize;
        VisualEnv = new VisualEnvironment(windowSize, shadowSize);
    }
}