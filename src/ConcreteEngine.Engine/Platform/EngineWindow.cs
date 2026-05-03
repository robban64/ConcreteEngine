using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace ConcreteEngine.Engine.Platform;

public sealed class EngineWindow
{
    private readonly IWindow _window;

    public bool PendingResize { get; private set; }

    public Size2D OutputSize { get; private set; }
    public Vector2 InvOutputSize { get; private set; }

    private Size2D _windowSize, _lastWindowSize;


    internal EngineWindow(IWindow window)
    {
        _window = window;
        OutputSize = _window.FramebufferSize.ToSize2D();
        _windowSize = _lastWindowSize = _window.Size.ToSize2D();
    }

    internal IWindow PlatformWindow => _window;

    internal bool Refresh()
    {
        _windowSize = _window.Size.ToSize2D();
        OutputSize = _window.FramebufferSize.ToSize2D();
        InvOutputSize = new Vector2(1.0f / OutputSize.Width, 1.0f / OutputSize.Height);

        var newSize = _windowSize != _lastWindowSize;
        var shouldResize = !newSize && PendingResize;
        PendingResize = newSize;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _windowSize;
        set
        {
            _windowSize = value;
            _window.Size = new Vector2D<int>(value.Width, value.Height);
        }
    }

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