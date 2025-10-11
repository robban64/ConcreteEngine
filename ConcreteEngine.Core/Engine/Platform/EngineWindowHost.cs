#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Error;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

#endregion

namespace ConcreteEngine.Core.Engine.Platform;

public interface IEngineWindowHost : IDisposable
{
    string Title { get; set; }
    Size2D FramebufferSize { get; }
    Size2D Size { get; set; }
    Vector2I Position { get; set; }

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
    public Vector2I Position
    {
        get => _window.Position.ToVector2I();
        set => _window.Position = new Vector2D<int>(value.X, value.Y);
    }

    public Size2D Size
    {
        get => _window.Size.ToSize2D();
        set => _window.Size = new Vector2D<int>(value.Width, value.Height);
    }

    public Size2D FramebufferSize => _window.FramebufferSize.ToSize2D();

    private GameEngineBuilder? _builder = null;

    public EngineWindowHost(
        WindowOptions options,
        GraphicsBackend backend)
    {
        _options = options;
        _backend = backend;
    }

    public void Run(GameEngineBuilder builder)
    {
        _builder = builder;

        _window = Window.Create(_options);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;

        _window.Run();
        _window.Dispose();
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

    private void OnLoad()
    {
        if (_builder == null) throw new InvalidOperationException("Builder not initialized");

        var graphics = _backend switch
        {
            GraphicsBackend.OpenGl => new GfxRuntimeBundle<GL>(new GraphicsRuntime(),
                new GlStartupConfig(_window.CreateOpenGL())),
            _ => throw new GraphicsException("Invalid GraphicsBackend. Only OpenGL supported")
        };

        _inputSource = new EngineInputSource(_window.CreateInput());
        _engine = _builder.Build(this, _inputSource, graphics);
        _builder = null;
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