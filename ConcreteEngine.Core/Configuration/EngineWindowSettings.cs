#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Configuration;

public record EngineWindowSettings
{
    Vector2D<int> Position { get; init; } = new(50, 50);
    Vector2D<int> Size { get; set; } = new(1280, 720);

    public string Title { get; init; }
}