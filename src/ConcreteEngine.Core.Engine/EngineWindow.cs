using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Common.Visuals;
using ConcreteEngine.Core.Diagnostics.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace ConcreteEngine.Core.Engine;

public static class EngineWindow
{
    public static bool IsDirty { get; private set; }
    public static Size2D WindowSize { get; private set; }
    public static Size2D OutputSize { get; private set; }

    private static ViewportRect _viewport, _nextViewport;
    private static Size2D _nextWindowSize, _nextOutputSize;

    private static IWindow _platformWindow = null!;

    public static nint PlatformWindowPtr => _platformWindow.Handle;

    public static ref readonly ViewportRect Viewport
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _viewport;
    }

    internal static void Attach(IWindow platformWindow)
    {
        if (_platformWindow != null)
            throw new InvalidOperationException("Only one EngineWindow can exist at a time.");

        ArgumentNullException.ThrowIfNull(platformWindow);

        _platformWindow = platformWindow;
        OutputSize = _nextOutputSize = _platformWindow.FramebufferSize.ToSize2D();
        WindowSize = _nextWindowSize = _platformWindow.Size.ToSize2D();
        _viewport = new ViewportRect(OutputSize);

        _platformWindow.Resize += OnResize;
        _platformWindow.FramebufferResize += OnOutputResize;
    }


    public static void SetViewport(ViewportRect vp)
    {
        if (vp == _nextViewport || vp == _viewport) return;

        if (vp.Size.IsNegativeOrZero() || vp.Size > OutputSize || vp.Position.IsNegative())
            throw new ArgumentOutOfRangeException(nameof(vp), $"Invalid viewport: {vp}");

        IsDirty = true;
        _nextViewport = vp;
    }

    internal static bool Commit()
    {
        if (!IsDirty) return false;
        IsDirty = false;

        _viewport = _nextViewport;
        WindowSize = _nextWindowSize;
        OutputSize = _nextOutputSize;
        Logger.LogString(LogScope.Engine, $"ScreenResized: {WindowSize}");

        return true;
    }

    public static string Title
    {
        get => _platformWindow.Title;
        set => _platformWindow.Title = value;
    }

    public static Vector2I Position
    {
        get => _platformWindow.Position.ToVec2Int();
        set => _platformWindow.Position = new Vector2D<int>(value.X, value.Y);
    }

    public static void SetWindowSize(Size2D windowSize)
    {
        if (_nextWindowSize == windowSize) return;
        _platformWindow.Size = new Vector2D<int>(windowSize.Width, windowSize.Height);
        IsDirty = true;
    }

    private static void OnResize(Vector2D<int> size)
    {
        _nextWindowSize = size.ToSize2D();
        IsDirty = true;
    }

    private static void OnOutputResize(Vector2D<int> size)
    {
        _nextOutputSize = size.ToSize2D();
        IsDirty = true;
    }

    public static void CenterOnCurrentMonitor()
    {
        var monitor = _platformWindow.Monitor;
        if (monitor is not null)
        {
            var area = monitor.Bounds;
            var size = _platformWindow.Size;
            var pos = new Vector2D<int>(
                area.Origin.X + Math.Max(0, (area.Size.X - size.X) / 2),
                area.Origin.Y + Math.Max(0, (area.Size.Y - size.Y) / 2)
            );
            _platformWindow.Position = pos;
        }
    }
}