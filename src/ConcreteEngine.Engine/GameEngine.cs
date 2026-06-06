using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Gateway;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Engine;

public sealed class GameEngine : IDisposable
{
    private readonly GraphicsRuntime _graphics;

    private readonly EngineTickHub _tickHub;

    private readonly EngineCoreSystem _coreSystems;
    private readonly EngineGateway _gateway;

    private bool _isDisposed;

    internal GameEngine(
        GfxRuntimeBundle<GL> gfxBundle,
        List<Func<GameScene>> sceneFactories
    )
    {
        _graphics = gfxBundle.Graphics;

        var gpuCapabilities = _graphics.Initialize(gfxBundle.Config, out var version);

        EngineSettings.Current.LoadGraphicsSettings(version, gpuCapabilities);

        Ecs.Init();

        _coreSystems = new EngineCoreSystem(gfxBundle.Graphics, sceneFactories);

        _gateway = new EngineGateway(_coreSystems);

        _tickHub = new EngineTickHub(OnGameTick, OnSimulate, _gateway.UpdateDiagnostics, _coreSystems.OnSystemTick);

        EngineSetupPipeline.Setup(new EngineSetupCtx
        {
            Graphics = _graphics,
            CoreSystem = _coreSystems,
            EngineGateway = _gateway,
            TickHub = _tickHub
        });
    }

    internal void RunSetup()
    {
        var runner = EngineSetupPipeline.Instance!;
        var isDone = runner.Run();
        EngineHost.IsSetupSimulation = runner.CurrentStep >= EngineSetupState.LoadEditor;

        _graphics.Gfx.Commands.Clear(ColorRgba.Black, ClearBufferFlag.ColorAndDepth);
        if (!isDone) return;

        Console.WriteLine("Engine Setup Complete. Swapping to Game Loop.");
        Logger.LogString(LogScope.Engine, "Engine Setup Complete. Swapping to Game Loop.");
        runner.Teardown();

        _coreSystems.OnSystemTick(0);
    }
    
    internal void Render(float dt)
    {
        _gateway.Metrics.StartCapture();

        // Update
        EngineInput.Update();
        _gateway.BeginFrame();

        _tickHub.Update(dt);
        _tickHub.AdvanceFrame(dt);

        // Draw
        Draw(dt);

        EngineInput.Keyboard.ClearKeys();

        _gateway.Metrics.EndCapture();
    }


    private void Draw(float dt)
    {
        var vp = EngineWindow.Viewport.Size;
        _graphics.BeginFrame(new GfxFrameArgs(dt, vp));
        _coreSystems.Render.Render(dt, vp, EngineInput.Mouse.ViewportPos);
        _graphics.EndFrame();

        _gateway.RenderEditor(dt);
    }


    private void OnGameTick(float dt)
    {
        CameraManager.Instance.BeginUpdate();
        
        _coreSystems.Scene.UpdateScene(dt);
        _gateway.UpdateGameTick(dt);
        _coreSystems.Render.AfterUpdate();
    }

    private void OnSimulate(float dt)
    {
        ParticleProcessor.Simulate(dt);
    }

    internal void Close()
    {
        if (_isDisposed) return;
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;
        Cleanup();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;
        Cleanup();
    }

    private void Cleanup()
    {
        _gateway.Dispose();
        _coreSystems.Dispose();
        EngineInput.Detach();
        _graphics.Dispose();
    }
}