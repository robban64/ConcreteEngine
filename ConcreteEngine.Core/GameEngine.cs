#region

using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Patterns;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Engine.Platform;
using ConcreteEngine.Core.Engine.Time;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Modules;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;
using Shader = ConcreteEngine.Core.Assets.Resources.Shader;

#endregion

namespace ConcreteEngine.Core;

public sealed class GameEngine : IDisposable
{
    private readonly IEngineWindowHost _window;
    private readonly GraphicsRuntime _graphics;

    private readonly EngineCoreSystem _coreSystems;
    private readonly AssetSystem _assets;
    private readonly InputSystem _inputSystem;
    private readonly RenderSystem _renderer;

    private readonly ModuleManager _modules;
    private readonly FeatureManager _features;
    private readonly SceneManager _sceneManager;

    private readonly UpdateFrameInfo _updateInfo = new();
    private readonly RenderFrameInfo _renderInfo = new();

    private readonly EngineTimeHub _timeHub;

    private bool _isDisposed = false;

    private LinearStateMachine<EngineStateLevel> _stateMachine;

    internal GameEngine(
        IEngineWindowHost windowHost,
        GfxRuntimeBundle<GL> gfxBundle,
        IEngineInputSource input,
        AssetManagerConfiguration assetConfig,
        List<Func<GameScene>> sceneFactories
    )
    {
        _window = windowHost;
        _graphics = gfxBundle.Graphics;

        _graphics.Initialize(gfxBundle.Config);

        _sceneManager = new SceneManager(sceneFactories);

        _modules = new ModuleManager();
        _features = new FeatureManager();

        // time
        _timeHub = new EngineTimeHub(GameTickUpdate, FpsTickUpdate, OnGpuTickUpload, OnGpuTickDispose);

        // input
        _inputSystem = new InputSystem(input);
        _assets = new AssetSystem(assetConfig.AssetPath, assetConfig.ManifestFilename);
        _renderer = new RenderSystem(_graphics, _window.FramebufferSize);
        _coreSystems = new EngineCoreSystem(_renderer, _inputSystem, _assets);

        _stateMachine = new LinearStateMachine<EngineStateLevel>(Enum.GetValues<EngineStateLevel>());
    }


    private void StartAssetLoader()
    {
        _assets.StartLoader(_graphics.Gfx);
    }

    private void InitializeSystems()
    {
        var materialStore = _assets.FinishLoading();
        _renderer.Initialize(materialStore, _assets);
        _coreSystems.Initialize();
    }

    internal void Render(float dt)
    {
        var fps = dt > 0 ? 1.0f / dt : 0.0f;
        _renderInfo.BeginFrame(fps, dt, _timeHub.Alpha, _window.Size, _window.FramebufferSize);
        _timeHub.RenderFrame(dt);


        if (_renderInfo.FrameIndex > 1 && _renderInfo.PrevOutputSize != _renderInfo.OutputSize)
        {
            _timeHub.DebounceTicker ??= new DebounceTicker(30);
        }


        if (_timeHub.DebounceTicker?.Tick() ?? false)
        {
            _timeHub.DebounceTicker = null;
            var fbos = _renderer.RenderRegistry.RenderFbos;
            Span<(FrameBufferId, Size2D)> newSizes = stackalloc (FrameBufferId, Size2D)[fbos.Count];
            for (int i = 0; i < fbos.Count; i++)
                newSizes[i] = (fbos[i].FboId, fbos[i].CalculateNewSize(_renderInfo.OutputSize));

            _graphics.RecreateFbo(newSizes);
            return;
        }

        var frameInfo = _renderInfo.Frame;
        _graphics.BeginFrame(in frameInfo);
        if (_sceneManager.Current != null)
        {
            // _renderTime.TickOrRenderEffect();
            _renderer.Render(_renderInfo.Alpha, in frameInfo);
        }

        //_renderTime.TickOrGpuDispose();
        //_renderTime.TickOrGpuUpload();
        _graphics.EndFrame(out var gfxFrameResult);

        _renderInfo.EndFrame(gfxFrameResult);
    }

    private void OnGpuTickDispose(int tick)
    {
    }

    private void OnGpuTickUpload(int tick)
    {
    }

    internal void Update(float dt)
    {
        _updateInfo.BeginFrame(dt, _window.Size, _window.FramebufferSize);

        if (_stateMachine.Current != EngineStateLevel.Running)
        {
            RunSetupStateMachine();
            return;
        }
        _timeHub.UpdateFrame(dt);

        var updateInfo = _updateInfo.UpdateInfo;
        _sceneManager.Current?.Update(in updateInfo);

        UpdateSceneTransitionIfNeeded();
    }

    private void GameTickUpdate(int tick)
    {
        _updateInfo.GameTick = tick;
        _renderer.BeginTick(_updateInfo.UpdateInfo);
        _inputSystem.Update();
        _sceneManager.Current?.UpdateTick(tick);
        _renderer.EndTick();
    }

    private void FpsTickUpdate(int tick)
    {
        var updateInfo = _updateInfo.UpdateInfo;
        var gfxResult = _renderInfo.GfxResult;

        Console.WriteLine(
            $"Fps: {updateInfo.Fps}; Draw Calls: {gfxResult.DrawCalls}; Triangle Count: {gfxResult.TriangleCount}");
    }


    private void RunSetupStateMachine()
    {
        switch (_stateMachine.Current)
        {
            case EngineStateLevel.NotStarted:
                _stateMachine.Next();
                break;
            case EngineStateLevel.LoadingGraphics:
                StartAssetLoader();
                _stateMachine.Next();
                break;
            case EngineStateLevel.LoadingAssets:
                _stateMachine.Next(_assets.ProcessLoader(8));
                break;
            case EngineStateLevel.InitializeSystem:
                var shaders = _assets.GetAll<Shader>();
                _renderer.InitializeGraphics(shaders);
                InitializeSystems();
                _stateMachine.Next();
                break;
            case EngineStateLevel.LoadScenes:
                _sceneManager.QueueSwitch(0);
                _stateMachine.Next();
                break;
        }
    }

    private void UpdateSceneTransitionIfNeeded()
    {
        if (!_sceneManager.HasPendingSwitch)
            return;

        var sceneContext = new GameSceneContext(_coreSystems) { Features = _features, Modules = _modules };
        var builder = new GameSceneConfigBuilder(_features, _modules);

        _sceneManager.ApplyPendingScene(sceneContext, builder, _renderer, AfterBuild);

        _features.Load(new GameFeatureContext(sceneContext));
        _modules.Load(new GameModuleContext(sceneContext));
        return;

        void AfterBuild(SceneManager.SceneBuildResult result, RenderSystem renderer)
        {
            renderer.RegisterScene(builder.RenderType, builder.RenderTargetsDesc);
            foreach (var module in result.Modules) result.Context.Modules.AddModule(module());
        }
    }

    internal void Close()
    {
        Console.WriteLine("Closing GameEngine");
        _isDisposed = true;
        _sceneManager.Current?.Unload();
        _assets?.Shutdown();
        // _graphics?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Console.WriteLine("Disposing GameEngine");
        _isDisposed = true;

        _assets?.Shutdown();
        //_graphics?.Dispose();
    }
}