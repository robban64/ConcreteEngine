using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Configuration;

internal sealed class EngineSettingsRecord
{
    public DisplaySettings Display { get; init; } =
        new(WindowSize: new Size2D(1280, 700), FrameRate: 80, Vsync: false, Fullscreen: false);

    public SimulationSettings Simulation { get; init; } =
        new(GameSimRate: 60, EnvironmentSimRate: 40, UiSimRate: 40, DiagnosticSimRate: 4);

    public GraphicsQualitySettings GraphicsQuality { get; init; } =
        new(ShadowQuality: GraphicsLevel.Unset, TextureQuality: GraphicsLevel.Unset);
}

public sealed class EngineSettings
{
    private const int MinScreenSize = 128;
    private const int MinFrameRate = 30;
    private const int MaxFrameRate = 512;

    public static readonly EngineSettings Instance = new();
    public static bool HasLoaded { get; private set; }

    internal static void LoadSettings(EngineSettingsRecord record)
    {
        HasLoaded = true;
        Instance.SetDisplaySettings(record.Display);
        Instance.SetDisplaySimulationSettings(record.Simulation);
        Instance.SetGraphicsQuality(record.GraphicsQuality);
    }

    public DisplaySettings Display { get; private set; }
    public SimulationSettings Simulation { get; private set; }
    public GraphicsQualitySettings GraphicsQuality { get; private set; }
    public GraphicsSettings Graphics { get; private set; }

    private EngineSettings()
    {
    }


    internal void SetDisplaySettings(in DisplaySettings display)
    {
        Display = display with
        {
            WindowSize = display.WindowSize.Clamp(new Size2D(MinScreenSize), new Size2D(10_000)),
            FrameRate = int.Clamp(display.FrameRate, MinFrameRate, MaxFrameRate)
        };
    }

    internal void SetDisplaySimulationSettings(in SimulationSettings sim)
    {
        var frameRate = Display.FrameRate;
        if (frameRate < MinFrameRate) throw new InvalidOperationException(nameof(frameRate));

        Simulation = new SimulationSettings
        {
            GameSimRate = int.Clamp(sim.GameSimRate, 1, frameRate),
            EnvironmentSimRate = int.Clamp(sim.EnvironmentSimRate, 1, frameRate),
            UiSimRate = int.Clamp(sim.UiSimRate, 1, frameRate),
            DiagnosticSimRate = int.Clamp(sim.DiagnosticSimRate, 1, frameRate),
        };
    }

    internal void SetGraphicsQuality(GraphicsQualitySettings graphicsQuality)
    {
        GraphicsQuality = graphicsQuality;
        Graphics = new GraphicsSettings
        {
            ShadowSize = graphicsQuality.ShadowQuality switch
            {
                GraphicsLevel.Low => 1024,
                GraphicsLevel.Unset or GraphicsLevel.Medium => 2048,
                GraphicsLevel.High => 4096,
                GraphicsLevel.Ultra => 8192,
                _ => throw new ArgumentOutOfRangeException()
            },
            MaxAnisotropy = graphicsQuality.TextureQuality switch
            {
                GraphicsLevel.Low => TextureAnisotropy.X2,
                GraphicsLevel.Unset or GraphicsLevel.Medium => TextureAnisotropy.X4,
                GraphicsLevel.High => TextureAnisotropy.X8,
                GraphicsLevel.Ultra => TextureAnisotropy.X16,
                _ => throw new ArgumentOutOfRangeException()
            }
        };
    }
}