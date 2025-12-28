using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace ConcreteEngine.Engine.Platform;

public sealed class EngineWindowHost
{
    private WindowOptions _options;
    private readonly GraphicsBackend _backend;

    private bool _disposed;

    private IWindow _window = null!;
    private EngineWindow _engineWindow = null!;

    private EngineInputSource _inputSource = null!;

    private GameEngine _engine = null!;

    public GraphicsBackend Backend => _backend;


    private GameEngineBuilder? _builder;

    public EngineWindowHost(
        WindowOptions options,
        GraphicsBackend backend)
    {
        _options = options;
        _backend = backend;
    }

    public void Run(GameEngineBuilder builder)
    {
        EngineSettingsLoader.LoadGraphicSettings();
        var display = EngineSettings.Instance.Display;
        var sim = EngineSettings.Instance.Simulation;
        
        _builder = builder;
        _options.Size = new Vector2D<int>(display.WindowSize.Width, display.WindowSize.Height);
        _options.VSync = display.Vsync;
        _options.UpdatesPerSecond = sim.GameSimRate;
        _options.FramesPerSecond = display.FrameRate;

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

    private void OnRender(double delta)
    {
        _engine.Render((float)delta);
    }


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