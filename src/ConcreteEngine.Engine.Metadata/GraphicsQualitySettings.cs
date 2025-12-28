using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Specs.Graphics;

namespace ConcreteEngine.Engine.Metadata;

public enum GraphicsLevel : byte
{
    Unset = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Ultra = 4
}

public struct EngineSettingsBundle
{
    public DisplaySettings Display;
    public GraphicsQualitySettings GraphicsQuality;
    public SimulationSettings Simulation;
}

public readonly record struct DisplaySettings(
    Size2D WindowSize,
    int FrameRate,
    bool Vsync,
    bool Fullscreen);

public readonly record struct SimulationSettings(
    int GameSimRate,
    int EnvironmentSimRate,
    int UiSimRate,
    int DiagnosticSimRate);

public readonly record struct GraphicsQualitySettings(
    GraphicsLevel ShadowQuality,
    GraphicsLevel TextureQuality);

public readonly record struct GraphicsSettings(int ShadowSize, TextureAnisotropy MaxAnisotropy);