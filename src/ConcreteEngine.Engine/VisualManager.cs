using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Configuration;

namespace ConcreteEngine.Engine;

internal sealed class VisualManager
{
    internal static readonly VisualManager Instance = new();

    public readonly VisualEnvironment VisualEnv;

    private VisualManager()
    {
        var shadowSize = EngineSettings.Instance.Graphics.ShadowSize;
        var windowSize = EngineSettings.Instance.Display.WindowSize;
        VisualEnv = new VisualEnvironment(windowSize, shadowSize);
    }

}