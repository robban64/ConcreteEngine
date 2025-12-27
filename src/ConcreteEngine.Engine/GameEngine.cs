using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Time;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Editor;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
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
    private readonly EditorEngineQueue _editorQueues;

    private FastRandom _rng = new(12323);

    private EngineSetupStepper _setupStepper = new(8);

    private RenderRuntimeParams _runtimeParams;

    private bool _isDisposed;

    internal GameEngine(
        EngineWindow engineWindow,
        GfxRuntimeBundle<GL> gfxBundle,
        EngineInputSource input,
        List<Func<GameScene>> sceneFactories
    )
    {
        _window = engineWindow;
        _graphics = gfxBundle.Graphics;

        _graphics.Initialize(gfxBundle.Config);
        PrimitiveMeshes.CreatePrimitives(_graphics.Gfx.Meshes);


        // systems
        _inputSystem = new InputSystem(input);
        _assets = new AssetSystem();

        _renderer = new RenderEngine(_graphics, PrimitiveMeshes.FsqQuad);

        _world = new World(engineWindow, _graphics, _renderer, _assets);
        _sceneManager = new SceneManager(sceneFactories, _assets, _world);

        _coreSystems = new EngineCoreSystem(_inputSystem, _assets, _world, _sceneManager);

        var driver = gfxBundle.Config.DriverContext;
        var portalArgs = new EditorPortalArgs(driver, engineWindow.PlatformWindow, input.InputContext);
        _engineGateway = new EngineGateway(in portalArgs);
        _editorQueues = new EditorEngineQueue(_world, _assets);

        // time
        _timeHub = new EngineTimeHub(UpdateTick, SimulationTickUpdate, LogTickUpdate);
        _profiler = new EngineSystemProfiler(AssetConfigLoader.GraphicSettings.RenderFps);

        EngineMetricHub.Attach(_profiler, _assets.Store, _sceneManager.SceneWorld, _world);
        Logger.Setup();
    }


    private void StartAssetLoader()
    {
        _assets.Initialize();
        _assets.StartLoader(_graphics.Gfx);
    }

    private void InitializeSystems()
    {
        _assets.FinishLoading();
        RegisterRenderer();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }


    internal void Render(float dt)
    {
        var mousePos = _inputSystem.InputSource.MousePosition;

        _timeHub.UpdateFrame(dt);

        _window.OnFrameStart(out var outputSize, out var windowSize);

        var frameInfo = new FrameInfo(EngineTime.FrameId, dt, EngineTime.GameAlpha, outputSize);

        var runtimeParams = _runtimeParams =
            new RenderRuntimeParams(windowSize, mousePos, EngineTime.Time, _rng.NextFloat());


        if (_sceneManager.Current is null)
        {
            _renderer.RenderEmptyFrame(frameInfo);
            _profiler.Tick();
            return;
        }


        var beginStatus = _window.UpdateCheckResized() ? BeginFrameStatus.Resize : BeginFrameStatus.None;
        if (EngineTime.FrameId > 1 && beginStatus == BeginFrameStatus.Resize)
            _timeHub.Debounce(int.Min(60, (int)frameInfo.Fps));

        beginStatus = _timeHub.TryTriggerDebounceResize() ? BeginFrameStatus.Resize : BeginFrameStatus.None;


        _world.PreRender(beginStatus, frameInfo, runtimeParams);
        _world.ExecuteFrame();
        _graphics.EndFrame();

        _engineGateway.RenderEditor(dt);

        if (!_sceneManager.Enabled)
            _graphics.Gfx.Commands.Clear(GfxPassClear.MakeColorDepthClear(Color4.Black));

        _profiler.Tick();
    }

    internal void Update(float dt)
    {
        EngineTime.UpdateId++;

        if (_setupStepper.Current != EngineStateLevel.Running)
        {
            RunSetupStateMachine();
            if (_setupStepper.Current < EngineStateLevel.Warmup) return;
        }

        if (_assets.PendingAssetCount > 0)
            _assets.ProcessPendingQueue(EngineTime.UpdateId);

        if (_editorQueues.QueuesCount > 0)
        {
            _editorQueues.DrainMainCommands();
            _editorQueues.DrainDeferredCommands();
        }

        if (_engineGateway.Active)
            _inputSystem.Update(!_engineGateway.BlockInput());

        _timeHub.Accumulate(dt);
        _timeHub.Advance();
    }

    private void UpdateTick(float dt)
    {
        _world.UpdateTick(dt, _window.OutputSize);
        if (_setupStepper.Current == EngineStateLevel.Running)
            _sceneManager.UpdateTick(dt);
        _world.EndUpdateTick(dt);
    }


    private void SimulationTickUpdate(float dt) => _world.OnSimulationTick(dt);

    private void LogTickUpdate(float dt) => _engineGateway.UpdateDiagnostics();

    private void RunSetupStateMachine()
    {
        switch (_setupStepper.Current)
        {
            case EngineStateLevel.NotStarted:
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadingGraphics:
                StartAssetLoader();
                _setupStepper.Next();
                break;
            case EngineStateLevel.LoadingAssets:
                _setupStepper.Next(_assets.ProcessLoader(8));
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
                Logger.SetupGfxLogger();
                if (_sceneManager.Current == null) throw new InvalidOperationException();
                _engineGateway.SetupEditor(_editorQueues, new ApiContext(_world, _assets, _sceneManager.SceneWorld));
                _setupStepper.Next();

                WarmupGenerics();
                break;
            case EngineStateLevel.Warmup:
                var result = _setupStepper.Next(++_setupStepper.WarmupTick > 60);
                if (result == EngineStateLevel.Running)
                    _sceneManager.SetEnabled(true);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupGenerics()
    {
        Ecs.Warmup();
        _graphics.Gfx.Commands.WarmUp();
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
        _assets.Shutdown();
        _engineGateway.Dispose();
        //_graphics?.Dispose();
    }
}