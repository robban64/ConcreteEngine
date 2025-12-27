using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace ConcreteEngine.Engine.Platform;

public sealed class EngineWindow
{
    private readonly IWindow _window;

    internal IWindow PlatformWindow => _window;

    private Size2D _prevOutputSize, _outputSize;
    private Size2D _windowSize;

    internal EngineWindow(IWindow window)
    {
        _window = window;
        _outputSize = _window.FramebufferSize.ToSize2D();
        _windowSize = _window.Size.ToSize2D();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void OnFrameStart(out Size2D outputSize, out Size2D windowSize)
    {
        windowSize = _windowSize = _window.Size.ToSize2D();
        outputSize = _outputSize = _window.FramebufferSize.ToSize2D();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool UpdateCheckResized()
    {
        bool hasResized = _outputSize != _prevOutputSize;
        _prevOutputSize = _outputSize;
        return hasResized;
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