using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace ConcreteEngine.Engine.Platform;

public sealed class EngineWindow
{
    private readonly IWindow _window;
    internal IWindow PlatformWindow => _window;

    private Size2D _outputSize;
    private Size2D _windowSize, _lastWindowSize;

    private bool _pendingResize;

    internal EngineWindow(IWindow window)
    {
        _window = window;
        _outputSize = _window.FramebufferSize.ToSize2D();
        _windowSize = _lastWindowSize = _window.Size.ToSize2D();
    }

    internal bool Refresh()
    {
        _windowSize = _window.Size.ToSize2D();
        _outputSize = _window.FramebufferSize.ToSize2D();

        var newSize = _windowSize != _lastWindowSize;
        var shouldResize = !newSize && _pendingResize;
        _pendingResize = newSize;

        _lastWindowSize = _windowSize;
        return shouldResize;
    }

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
        get => _windowSize;
        set
        {
            _windowSize = value;
            _window.Size = new Vector2D<int>(value.Width, value.Height);
        }
    }

    public Size2D OutputSize => _outputSize;

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