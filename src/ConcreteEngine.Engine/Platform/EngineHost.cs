using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace ConcreteEngine.Engine.Platform;

public sealed class EngineHost
{
    private sealed class SetupContainer(in WindowOptions options)
    {
        public GameEngineBuilder? Builder;
        public WindowOptions Options = options;
    }

    private SetupContainer? _setup;

    private bool _disposed;

    private IWindow _window = null!;
    private EngineWindow _engineWindow = null!;
    private EngineInputSource _inputSource = null!;
    private GameEngine _engine = null!;

    public GraphicsBackend Backend { get; }

    public EngineHost(WindowOptions options, GraphicsBackend backend)
    {
        _setup = new SetupContainer(in options);
        Backend = backend;
    }

    public void Run(GameEngineBuilder builder)
    {
        EngineSettingsLoader.LoadGraphicSettings();
        var display = EngineSettings.Instance.Display;

        _setup!.Builder = builder;
        _setup.Options.Size = new Vector2D<int>(display.WindowSize.Width, display.WindowSize.Height);
        _setup.Options.VSync = display.Vsync;
        _setup.Options.UpdatesPerSecond = display.FrameRate;
        _setup.Options.FramesPerSecond = display.FrameRate;

        _window = Window.Create(_setup.Options);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;

        _window.Run();
        _window.Dispose();
    }

    private void OnLoad()
    {
        if (_setup!.Builder == null) throw new InvalidOperationException("Builder not initialized");

        var graphics = Backend switch
        {
            GraphicsBackend.OpenGl => new GfxRuntimeBundle<GL>(new GraphicsRuntime(),
                new GlStartupConfig(_window.CreateOpenGL())),
            _ => throw new GraphicsException("Invalid GraphicsBackend. Only OpenGL supported")
        };

        _engineWindow = new EngineWindow(_window);
        _inputSource = new EngineInputSource(_window.CreateInput());
        _engine = _setup.Builder.Build(_engineWindow, _inputSource, graphics);
        _setup.Builder = null;
        _setup = null;
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