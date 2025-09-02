#region

using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.OpenGL;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

#endregion

namespace ConcreteEngine.Core.Platform;

public interface IEngineWindowHost : IDisposable
{
    string Title { get; set; }
    Vector2D<int> FramebufferSize { get; }
    Vector2D<int> Size { get; set; }
    Vector2D<int> Position { get; set; }

    GraphicsBackend Backend { get; }

    void CenterOnCurrentMonitor();
}

public sealed class EngineWindowHost : IEngineWindowHost
{
    private readonly WindowOptions _options;
    private readonly GraphicsBackend _backend;

    private IWindow _window = null!;
    private bool _disposed;

    private EngineInputSource _inputSource = null!;

    private GameEngine _engine = null!;


    public GraphicsBackend Backend => _backend;

    public string Title
    {
        get => _window.Title;
        set => _window.Title = value;
    }

    // Placement / size
    public Vector2D<int> Position
    {
        get => _window.Position;
        set => _window.Position = value;
    }

    public Vector2D<int> Size
    {
        get => _window.Size;
        set => _window.Size = value;
    }

    public Vector2D<int> FramebufferSize => _window.FramebufferSize;


    public EngineWindowHost(
        WindowOptions options,
        GraphicsBackend backend)
    {
        _options = options;
        _backend = backend;
    }

    public void Run(GameEngineBuilder builder)
    {
        _window = Window.Create(_options);

        _window.Load += () => OnLoad(builder);
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;

        _window.Run();
        _window.Dispose();
    }

    public void CenterOnCurrentMonitor()
    {
        // Basic centering using current monitor’s bounds if available
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

    private void OnLoad(GameEngineBuilder builder)
    {
        // Build graphics from the real GL context
        var initialFrameContext = new FrameMetaInfo
        {
            DeltaTime = 0,
            FramebufferSize = _window.FramebufferSize,
            ViewportSize = _window.Size
        };

        IGraphicsDevice graphics = _backend switch
        {
            GraphicsBackend.OpenGL => new GlGraphicsDevice(_window.CreateOpenGL(), in initialFrameContext),
            _ => throw new GraphicsException("Invalid GraphicsBackend. Only OpenGL supported")
        };


        _inputSource = new EngineInputSource(_window.CreateInput());

        _engine = builder.Build(this, _inputSource, graphics);
    }

    private void OnUpdate(double delta) => _engine.Update((float)delta);
    private void OnRender(double delta) => _engine.Render((float)delta);


    private void OnClosing()
    {
        Console.WriteLine("Closing...");
        _engine.Close();
        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _engine.Close();
        _inputSource.Dispose();
        _window?.Dispose();
        _disposed = true;
    }
}