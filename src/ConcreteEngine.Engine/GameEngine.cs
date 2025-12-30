using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Editor;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Metadata.Command;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.State;
using Silk.NET.OpenGL;
using Shader = ConcreteEngine.Engine.Assets.Shaders.Shader;

namespace ConcreteEngine.Engine;

public sealed class GameEngine : IDisposable
{
    private readonly GraphicsRuntime _graphics;
    private readonly RenderEngine _renderer;

    private readonly EngineWindow _window;
    private readonly EngineTimeHub _timeHub;
    private readonly EngineSystemProfiler _profiler;

    private readonly EngineCoreSystem _coreSystems;
    private readonly AssetSystem _assets;
    private readonly InputSystem _inputSystem;

    private readonly World _world;
    private readonly SceneManager _sceneManager;

    private readonly EngineGateway _engineGateway;
    private readonly EngineCommandQueue _commandQueues;

    private FastRandom _rng = new(12323);

    private EngineSetupStepper _setupStepper = new(8);

    private bool _isDisposed;

    internal GameEngine(
        EngineWindow window,
        GfxRuntimeBundle<GL> gfxBundle,
        EngineInputSource input,
        List<Func<GameScene>> sceneFactories
    )
    {
        _window = window;
        _graphics = gfxBundle.Graphics;

        var version = _graphics.Initialize(gfxBundle.Config, out var caps);

        EngineSettings.Instance.LoadGraphicsSettings(version, in caps);

        PrimitiveMeshes.CreatePrimitives(_graphics.Gfx.Meshes);

        // systems
        _inputSystem = new InputSystem(input);
        _assets = new AssetSystem();

        _renderer = new RenderEngine(_graphics, PrimitiveMeshes.FsqQuad);

        _world = new World(window, _graphics, _renderer, _assets);
        _sceneManager = new SceneManager(sceneFactories, _assets, _world);

        _coreSystems = new EngineCoreSystem(_inputSystem, _assets, _world, _sceneManager);

        var driver = gfxBundle.Config.DriverContext;
        _engineGateway = new EngineGateway(new EditorPortalArgs(driver, window.PlatformWindow, input.InputContext));
        _commandQueues = new EngineCommandQueue(_world, _assets);

        // time
        _timeHub = new EngineTimeHub(OnGameTick, OnEnvironmentTick, OnDiagnosticTick, OnSystemTick);
        _profiler = new EngineSystemProfiler();

    }


    private void StartAssetLoader()
    {
        _assets.Initialize();
        _assets.StartLoader(_graphics.Gfx);
    }

    private void InitializeSystems()
    {
        EngineMetricHub.Attach(_profiler, _assets.Store, _sceneManager.SceneWorld, _world);
        Logger.Setup();
    }

    private void OnSystemTick(float dt)
    {
        var pendingResize = _window.Refresh();
        if (pendingResize)
        {
            var command = new RenderCommandRecord(CommandRenderAction.RecreateScreenDependentFbo, _window.OutputSize);
            _commandQueues.EnqueueDeferred(new EngineCommandPackage(command));
        }
    }

    internal void Render(float dt)
    {
        _timeHub.BeginFrame(dt);

        if (_setupStepper.Current != EngineStateLevel.Running)
        {
            RunSetupStateMachine(dt);
            if (_setupStepper.Current < EngineStateLevel.Warmup) return;
        }

        if (_sceneManager.Current is null)
        {
            _graphics.BeginFrame(new GfxFrameArgs(EngineTime.FrameId, dt, _window.OutputSize));
            _graphics.EndFrame();
            _profiler.Tick();
            return;
        }

        var mousePos = _inputSystem.InputSource.MousePosition;
        var frameInfo = new FrameInfo(EngineTime.FrameId, dt, EngineTime.GameAlpha, _window.OutputSize);
        var runtimeParams = new RenderRuntimeParams(_window.WindowSize, mousePos, EngineTime.Time, _rng.NextFloat());

        _graphics.BeginFrame(frameInfo.ToGfxFrameInfo());
        _renderer.PrepareFrame(in frameInfo, in runtimeParams);

        _world.PreRender();
        _renderer.Render();

        _graphics.EndFrame();

        _engineGateway.RenderEditor(dt);

        if (!_sceneManager.Enabled)
            _graphics.Gfx.Commands.Clear(new GfxPassClear(Color.Black, ClearBufferFlag.ColorAndDepth));

        _profiler.Tick();
    }

    internal void Update(float dt)
    {
        if (_setupStepper.Current < EngineStateLevel.Warmup) return;
        _timeHub.Accumulate(dt);
        _timeHub.Advance();
    }

    private void OnGameTick(float dt)
    {
        EngineTime.GameTickId++;

        if (_assets.PendingAssetCount > 0)
            _assets.ProcessPendingQueue(EngineTime.GameTickId);

        if (_commandQueues.QueuesCount > 0)
        {
            _commandQueues.DrainMainCommands();
            _commandQueues.DrainDeferredCommands();
        }

        if (_engineGateway.Active)
            _inputSystem.Update(!_engineGateway.BlockInput());

        _world.UpdateTick(dt, _window.OutputSize);

        if (_setupStepper.Current == EngineStateLevel.Running)
            _sceneManager.UpdateTick(dt);

        _world.EndUpdateTick(dt);
    }


    private void OnEnvironmentTick(float dt) => _world.OnSimulationTick(dt);

    private void OnDiagnosticTick(float dt)
    {
        _engineGateway.UpdateDiagnostics();
    }

    private void RunSetupStateMachine(float dt)
    {
        switch (_setupStepper.Current)
        {
            case EngineStateLevel.NotStarted:
                StartAssetLoader();
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadingAssets:
                if (!_assets.ProcessLoader()) break;
                _assets.FinishLoading();
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadingGraphics:
                RegisterRenderer();
                _setupStepper.Next();
                break;
            case EngineStateLevel.InitializeSystem:
                InitializeSystems();
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadWorld:
                InitializeWorld();
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadEditor:
                LoadScene();
                if (_sceneManager.Current == null) throw new InvalidOperationException();
                EngineWarmup.WarmUp(_graphics);
                _engineGateway.SetupEditor(_commandQueues, new ApiContext(_world, _assets, _sceneManager.SceneWorld));
                Logger.ToggleGfxLog(true);
                _setupStepper.Next();
                break;
            case EngineStateLevel.Warmup:
                _setupStepper.WarmupTime += dt;
                var result = _setupStepper.Next(_setupStepper.WarmupTime >= 1);
                if (result == EngineStateLevel.Running)
                    _sceneManager.SetEnabled(true);
                break;
        }
    }


    private void InitializeWorld()
    {
        _sceneManager.QueueSwitch(0);
        _world.Initialize(_assets, _graphics.Gfx);
    }

    private void LoadScene()
    {
        if (!_sceneManager.HasPendingSwitch) return;
        var builder = new GameSceneConfigBuilder();
        _sceneManager.ApplyPendingScene(builder, _coreSystems);
    }

    private void RegisterRenderer()
    {
        var builder = _renderer.StartBuilder(_window.OutputSize);
        var shaderCount = _assets.Store.GetMetaSnapshot<Shader>().Count;

        builder.RegisterShader(shaderCount, ExtractShaderIds).RegisterCoreShaders(GetCoreShaders);
        WorldRenderSetup.RegisterFrameBuffers(builder, _world.WorldVisual);
        builder.SetupPassPipeline(RenderPipelineVersion.Default3D);
        _renderer.ApplyBuilder(builder);
        return;

        void ExtractShaderIds(Span<ShaderId> span) =>
            _assets.Store.ExtractSpan<Shader, ShaderId>(span, static shader => shader.ResourceId);

        RenderCoreShaders GetCoreShaders() => WorldRenderSetup.GetCoreShaders(_assets.Store);
    }

    internal void Close()
    {
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;
        _engineGateway.Dispose();
        _sceneManager.Current?.Unload();
        _assets.Shutdown();
        // _graphics?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;
        _engineGateway.Dispose();
        _assets.Shutdown();
        //_graphics?.Dispose();
    }
}