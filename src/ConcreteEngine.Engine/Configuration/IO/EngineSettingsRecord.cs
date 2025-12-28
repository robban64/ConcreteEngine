using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Configuration.IO;

internal sealed class EngineSettingsRecord
{
    public DisplaySettings Display { get; init; } =
        new(WindowSize: new Size2D(1280, 700), FrameRate: 80, Vsync: false, Fullscreen: false);

    public SimulationSettings Simulation { get; init; } =
        new(GameSimRate: 60, EnvironmentSimRate: 40, UiSimRate: 40, DiagnosticSimRate: 4);

    public GraphicsQualitySettings GraphicsQuality { get; init; } =
        new(ShadowQuality: GraphicsLevel.Unset, TextureQuality: GraphicsLevel.Unset);
}
