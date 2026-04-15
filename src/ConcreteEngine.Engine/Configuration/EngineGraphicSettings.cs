using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Gateway.Diagnostics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Configuration;

public sealed class EngineSettings
{
    private const int MinScreenSize = 128;
    private const int MinFrameRate = 30;
    private const int MaxFrameRate = 512;

    public static readonly EngineSettings Instance = new();

    internal static void LoadSettings(EngineSettingsRecord record)
    {
        Instance.SetDisplaySettings(record.Display);
        Instance.SetDisplaySimulationSettings(record.Simulation);
        Instance.SetGraphicsQuality(record.GraphicsQuality);
        Instance.HasLoaded = true;
    }

    public bool HasLoaded { get; private set; }

    public double FrameDelta => 1.0 / Display.FrameRate;

    public DisplaySettings Display { get; private set; }
    public SimulationSettings Simulation { get; private set; }
    public GraphicsQualitySettings GraphicsQuality { get; private set; }
    public GraphicsSettings Graphics { get; private set; }
    public OpenGlVersion OpenGlVersion { get; private set; }
    public GpuDeviceCapabilities GpuCapabilities { get; private set; } = null!;


    private EngineSettings()
    {
    }


    internal void LoadGraphicsSettings(OpenGlVersion version, GpuDeviceCapabilities caps)
    {
        OpenGlVersion = version;
        GpuCapabilities = caps;

        var str = $"OpenGL version {OpenGlVersion} loaded.";
        Logger.LogString(LogScope.Gfx, str, LogLevel.Info);
    }

    internal EngineSettingsRecord GetSettingsRecord()
    {
        if (!HasLoaded) throw new InvalidOperationException(nameof(HasLoaded));
        return new EngineSettingsRecord
        {
            Display = Display, Simulation = Simulation, GraphicsQuality = GraphicsQuality
        };
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