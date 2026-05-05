using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Renderer.Data;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace ConcreteEngine.Engine.Platform;

public sealed class EngineWindow
{
    private readonly IWindow _window;
    public bool IsDirty { get; private set; }
    public Size2D OutputSize { get; private set; }

    private ViewportRect _viewport;
    private Size2D _windowSize, _lastWindowSize;

    internal EngineWindow(IWindow window)
    {
        _window = window;
        OutputSize = _window.FramebufferSize.ToSize2D();
        _windowSize = _lastWindowSize = _window.Size.ToSize2D();
        _viewport = new ViewportRect(OutputSize);
    }


    internal IWindow PlatformWindow => _window;

    public ref readonly ViewportRect Viewport
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _viewport;
    }

    internal void UpdateViewport(ViewportRect vp)
    {
        if (vp == _viewport) return;

        if (vp.Size.IsNegativeOrZero() || vp.Position.IsNegative())
            throw new ArgumentOutOfRangeException(nameof(vp), $"Invalid viewport: {vp}");

        _viewport = vp;
        IsDirty = true;
    }

    internal bool Refresh()
    {
        _windowSize = _window.Size.ToSize2D();
        OutputSize = _window.FramebufferSize.ToSize2D();

        // var newSize = _windowSize != _lastWindowSize;
        // var shouldResize = !newSize && IsDirty;
        // IsDirty = newSize;

        var isDirty = IsDirty;
        if (!isDirty) isDirty = _windowSize != _lastWindowSize;
        _lastWindowSize = _windowSize;

        IsDirty = false;
        return isDirty;
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