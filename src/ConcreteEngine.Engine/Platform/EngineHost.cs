using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Configuration.Setup;
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

    internal static bool IsSetupSimulation = false;
    internal static bool IsSetup = true;


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
        _window.Initialize();

        OnLoad();

        RunSetupLoop();
        RunMainLoop();

        OnClosing();
        _window.Dispose();
    }

    private void RunSetupLoop()
    {
        double lastTime = _window.Time;
        
        while (!_window.IsClosing)
        {
            if (!IsSetup) return;

            _window.DoEvents();

            var currentTime = _window.Time;
            var deltaTime = currentTime - lastTime;
            lastTime = currentTime;

            _engine.RunSetup(deltaTime);
            if (IsSetupSimulation) 
                _engine.Update(0);

            _window.SwapBuffers();
        }
    }

    private void RunMainLoop()
    {
        double lastTime = _window.Time;
        while (!_window.IsClosing)
        {
            _window.DoEvents();

            var currentTime = _window.Time;
            var deltaTime = currentTime - lastTime;
            lastTime = currentTime;

            if (_window.WindowState == WindowState.Minimized)
            {
                Thread.Sleep(100);
                continue;
            }

            _engine.Update(deltaTime);
            _engine.Render(deltaTime);

            _window.SwapBuffers();
        }
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