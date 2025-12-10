#region

using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

#endregion

namespace ConcreteEngine.Engine.Platform;

public sealed class EngineWindowHost
{
    private readonly WindowOptions _options;
    private readonly GraphicsBackend _backend;

    private bool _disposed;

    private IWindow _window = null!;
    private EngineWindow _engineWindow = null!;

    private EngineInputSource _inputSource = null!;

    private GameEngine _engine = null!;

    public IWindow InternalWindow => _window;

    public GraphicsBackend Backend => _backend;


    private GameEngineBuilder? _builder = null;

    public EngineWindowHost(
        WindowOptions options,
        GraphicsBackend backend)
    {
        _options = options;
        _backend = backend;
        _options.VSync = false;
        _options.UpdatesPerSecond = 60;
        _options.FramesPerSecond = 144;
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

    private void OnLoad()
    {
        if (_builder == null) throw new InvalidOperationException("Builder not initialized");

        var graphics = _backend switch
        {
            GraphicsBackend.OpenGl => new GfxRuntimeBundle<GL>(new GraphicsRuntime(),
                new GlStartupConfig(_window.CreateOpenGL())),
            _ => throw new GraphicsException("Invalid GraphicsBackend. Only OpenGL supported")
        };

        _engineWindow = new EngineWindow(_window);
        _inputSource = new EngineInputSource(_window.CreateInput());
        _engine = _builder.Build(_engineWindow, _inputSource, graphics);
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