#region

using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Core.Configuration;

public record EngineWindowSettings
{
    Bounds2D Bounds { get; set; } = new(0, 0, 1280, 720);
    public Size2D Size => Bounds.ToSize2D();

    public string Title { get; init; }
}