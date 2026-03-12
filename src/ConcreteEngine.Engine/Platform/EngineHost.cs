using System.Diagnostics;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Configuration.Setup;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

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

    private readonly Stopwatch _renderSw = new();

    private SetupContainer? _setup;

    private IWindow _window = null!;
    private EngineWindow _engineWindow = null!;
    private EngineInputSource _inputSource = null!;
    private GameEngine _engine = null!;

    public GraphicsBackend Backend { get; }

    private bool _disposed;
    private double _lastFrameTime;

    public EngineHost(WindowOptions options, GraphicsBackend backend)
    {
        _setup = new SetupContainer(in options);
        Backend = backend;

        _renderSw.Start();
    }

    public void Run(GameEngineBuilder builder)
    {
        EngineSettingsLoader.LoadGraphicSettings();
        var display = EngineSettings.Instance.Display;


        _setup!.Builder = builder;
        _setup.Options.Size = new Vector2D<int>(display.WindowSize.Width, display.WindowSize.Height);
        _setup.Options.VSync = false;
        _setup.Options.UpdatesPerSecond = 0;
        _setup.Options.FramesPerSecond = 0;

        GlfwWindowing.Use();

        _window = Window.Create(_setup.Options);
        _window.Initialize();

        OnLoad();

        _window.VSync = false;
        RunSetupLoop();
        RunMainLoop();

        OnClosing();
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

        if (_window.GLContext != null)
        {
            _window.GLContext.MakeCurrent();

            var glfw = Glfw.GetApi();
            glfw.SwapInterval(0);
        }

        _engineWindow = new EngineWindow(_window);
        _inputSource = new EngineInputSource(_window.CreateInput());
        _engine = _setup.Builder.Build(_engineWindow, _inputSource, graphics);
        _setup.Builder = null;
        _setup = null;
    }

    private void RunSetupLoop()
    {
        var frameCap = TimeSpan.FromMilliseconds(16);

        while (!_window.IsClosing)
        {
            if (!IsSetup) return;

            var start = _renderSw.Elapsed;

            _window.DoEvents();
            _engine.RunSetup(0);
            //if (IsSetupSimulation)
            //    _engine.Update(0);

            _window.SwapBuffers();
            var duration = _renderSw.Elapsed - start;
            var sleep = frameCap - duration;
            if (sleep.TotalMilliseconds > 0)
            {
                Thread.Sleep(sleep);
            }
        }
    }

    private void RunMainLoop()
    {
        const double maxDelta = 0.1;

        var targetFrameTime = EngineSettings.Instance.FrameDelta;
        var previousTime = _renderSw.Elapsed.TotalSeconds;

        while (!_window.IsClosing)
        {
            if (_window.WindowState == WindowState.Minimized)
            {
                Thread.Sleep(100);
                continue;
            }

            var currentTime = _renderSw.Elapsed.TotalSeconds;
            var deltaTime = currentTime - previousTime;
            if (deltaTime > maxDelta) deltaTime = maxDelta;

            previousTime = currentTime;


            _window.DoEvents();

            _engine.Render(deltaTime);

            _window.SwapBuffers();

            var targetNextFrame = currentTime + targetFrameTime;

            while (_renderSw.Elapsed.TotalSeconds < targetNextFrame)
            {
                var remaining = targetNextFrame - _renderSw.Elapsed.TotalSeconds;

                // vacation time, Sleep to save CPU.
                if (remaining > 0.002)
                    Thread.Sleep(1);
                else
                    Thread.SpinWait(10);
            }
        }
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