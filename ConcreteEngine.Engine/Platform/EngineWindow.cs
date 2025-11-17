#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Extensions;
using Silk.NET.Maths;
using Silk.NET.Windowing;

#endregion

namespace ConcreteEngine.Engine.Platform;

public interface IEngineWindow
{
    string Title { get; set; }
    Size2D OutputSize { get; }
    Size2D WindowSize { get; set; }
    Vector2I Position { get; set; }

    void CenterOnCurrentMonitor();
}

internal sealed class EngineWindow : IEngineWindow
{
    private readonly IWindow _window;

    internal IWindow PlatformWindow => _window;

    internal EngineWindow(IWindow window) => _window = window;

    public string Title
    {
        get => _window.Title;
        set => _window.Title = value;
    }

    public Vector2I Position
    {
        get => _window.Position.ToVec2Int();
        set => _window.Position = new Vector2D<int>(value.X, value.Y);
    }

    public Size2D WindowSize
    {
        get => _window.Size.ToSize2D();
        set => _window.Size = new Vector2D<int>(value.Width, value.Height);
    }

    public Size2D OutputSize => _window.FramebufferSize.ToSize2D();

    public void CenterOnCurrentMonitor()
    {
        var monitor = _window.Monitor;
        if (monitor is not null)
        {
            var area = monitor.Bounds;
            var size = _window.Size;
            var pos = new Vector2D<int>(
                area.Origin.X + Math.Max(0, (area.Size.X - size.X) / 2),
                area.Origin.Y + Math.Max(0, (area.Size.Y - size.Y) / 2)
            );
            _window.Position = pos;
        }
    }
}