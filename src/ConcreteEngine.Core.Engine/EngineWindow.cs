using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Common.Visuals;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace ConcreteEngine.Core.Engine;

public sealed class EngineWindow
{
    public static EngineWindow Current { get; private set; } = null!;

    internal IWindow PlatformWindow { get; }

    public bool IsDirty { get; private set; }
    public Size2D OutputSize { get; private set; }

    private ViewportRect _viewport, _nextViewport;
    private Size2D _windowSize, _lastWindowSize;

    internal EngineWindow(IWindow window)
    {
        if (Current is not null)
            throw new InvalidOperationException("Only one EngineWindow can exist at a time.");

        PlatformWindow = window;
        OutputSize = PlatformWindow.FramebufferSize.ToSize2D();
        _windowSize = _lastWindowSize = PlatformWindow.Size.ToSize2D();
        _viewport = new ViewportRect(OutputSize);
        Current = this;
    }

    public nint PlatformWindowPtr => PlatformWindow.Handle;

    public ref readonly ViewportRect Viewport
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _viewport;
    }

    public void SetViewport(ViewportRect vp)
    {
        if (vp == _nextViewport || vp == _viewport) return;

        if (vp.Size.IsNegativeOrZero() || vp.Size > OutputSize || vp.Position.IsNegative())
            throw new ArgumentOutOfRangeException(nameof(vp), $"Invalid viewport: {vp}");

        IsDirty = true;
        _nextViewport = vp;
    }

    internal bool Refresh()
    {
        _windowSize = PlatformWindow.Size.ToSize2D();
        OutputSize = PlatformWindow.FramebufferSize.ToSize2D();

        var isDirty = IsDirty;
        if (isDirty) _viewport = _nextViewport;
        else isDirty = _windowSize != _lastWindowSize;

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