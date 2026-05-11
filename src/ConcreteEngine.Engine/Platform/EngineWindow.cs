using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Renderer.Data;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace ConcreteEngine.Engine.Platform;

public sealed class EngineWindow
{
    internal IWindow PlatformWindow { get; }

    public bool IsDirty { get; private set; }
    public Size2D OutputSize { get; private set; }

    private ViewportRect _viewport;
    private Size2D _windowSize, _lastWindowSize;

    internal EngineWindow(IWindow window)
    {
        PlatformWindow = window;
        OutputSize = PlatformWindow.FramebufferSize.ToSize2D();
        _windowSize = _lastWindowSize = PlatformWindow.Size.ToSize2D();
        _viewport = new ViewportRect(OutputSize);
    }

    public ref readonly ViewportRect Viewport
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _viewport;
    }

    internal void UpdateViewport(ViewportRect vp)
    {
        if (vp == _viewport) return;

        if (vp.Size.IsNegativeOrZero() || vp.Size > OutputSize || vp.Position.IsNegative())
            throw new ArgumentOutOfRangeException(nameof(vp), $"Invalid viewport: {vp}");

        IsDirty = true;
        _viewport = vp;
    }

    internal bool Refresh()
    {
        _windowSize = PlatformWindow.Size.ToSize2D();
        OutputSize = PlatformWindow.FramebufferSize.ToSize2D();

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
        get => PlatformWindow.Title;
        set => PlatformWindow.Title = value;
    }

    public Vector2I Position
    {
        get => PlatformWindow.Position.ToVec2Int();
        set => PlatformWindow.Position = new Vector2D<int>(value.X, value.Y);
    }

    public Size2D WindowSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _windowSize;
        set
        {
            _windowSize = value;
            PlatformWindow.Size = new Vector2D<int>(value.Width, value.Height);
        }
    }

    public void CenterOnCurrentMonitor()
    {
        var monitor = PlatformWindow.Monitor;
        if (monitor is not null)
        {
            var area = monitor.Bounds;
            var size = PlatformWindow.Size;
            var pos = new Vector2D<int>(
                area.Origin.X + Math.Max(0, (area.Size.X - size.X) / 2),
                area.Origin.Y + Math.Max(0, (area.Size.Y - size.Y) / 2)
            );
            PlatformWindow.Position = pos;
        }
    }
}