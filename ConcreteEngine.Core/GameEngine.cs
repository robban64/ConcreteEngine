#region

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
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Producers;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using Silk.NET.OpenGL;
using RenderFrameInfo = ConcreteEngine.Core.Engine.Data.RenderFrameInfo;
using Shader = ConcreteEngine.Core.Assets.Shaders.Shader;

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
        _assets = new AssetSystem();
        _renderer = new RenderSystem(_graphics);
        _coreSystems = new EngineCoreSystem(_renderer, _inputSystem, _assets);

        _stateMachine = new LinearStateMachine<EngineStateLevel>(Enum.GetValues<EngineStateLevel>());
    }


    private void StartAssetLoader()
    {
        _assets.Initialize();
        _assets.StartLoader(_graphics.Gfx);
    }

    private void InitializeSystems()
    {
        _assets.FinishLoading();
        _coreSystems.Initialize();
        RegisterRenderer();
    }

    private void InitializeGraphics()
    {
        var store = _assets.Store;
/*
        var shaderCount = store.GetMetaSnapshot<Shader>().Count;
        Span<ShaderId> shaderIds = stackalloc ShaderId[shaderCount];
        store.ExtractSpan<Shader, ShaderId>(shaderIds, static shader => shader.ResourceId);

        _renderer.InitializeRegistry(shaderIds, new RenderCoreShaders
        {
            DepthShader = store.GetByName<Shader>("Depth").ResourceId,
            ColorFilterShader = store.GetByName<Shader>("ColorFilter").ResourceId,
            CompositeShader = store.GetByName<Shader>("Composite").ResourceId,
            PresentShader = store.GetByName<Shader>("Present").ResourceId
        });
*/
    }


    public void RegisterRenderer()
    {
        var builder = _renderer.StartBuilder(_window.OutputSize);
        builder.SetupRegistry((registry) =>
        {
            int shaderCount = _assets.Store.GetMetaSnapshot<Shader>().Count;

            registry.RegisterShader(shaderCount,
                    (span) => _assets.Store.ExtractSpan<Shader, ShaderId>(span, static shader => shader.ResourceId))
                .RegisterCoreShaders(() => new RenderCoreShaders
                {
                    DepthShader = _assets.Store.GetByName<Shader>("Depth").ResourceId,
                    ColorFilterShader = _assets.Store.GetByName<Shader>("ColorFilter").ResourceId,
                    CompositeShader = _assets.Store.GetByName<Shader>("Composite").ResourceId,
                    PresentShader = _assets.Store.GetByName<Shader>("Present").ResourceId
                });

            registry.RegisterFbo<ShadowPassTag>(FboVariant.Default,
                new RegisterFboEntry().AttachDepthTexture(GfxFboDepthTextureDesc.Default())
                    .UseFixedSize(new Size2D(2048, 2048)));

            registry.RegisterFbo<ScenePassTag>(FboVariant.Default,
                new RegisterFboEntry().AttachColorTexture(GfxFboColorTextureDesc.Off(), RenderBufferMsaa.X4)
                    .AttachDepthStencilBuffer());

            registry.RegisterFbo<ScenePassTag>(FboVariant.Secondary,
                new RegisterFboEntry()
                    .AttachColorTexture(GfxFboColorTextureDesc.DefaultMip())
                    .AttachDepthStencilBuffer());

            registry.RegisterFbo<PostPassTag>(FboVariant.Default,
                new RegisterFboEntry().AttachColorTexture(GfxFboColorTextureDesc.Default()));

            registry.RegisterFbo<PostPassTag>(FboVariant.Secondary,
                new RegisterFboEntry().AttachColorTexture(GfxFboColorTextureDesc.Default()));
        });

        builder.SetupBatchers((gfx, batchers) =>
        {
            batchers.Register(new TerrainBatcher(gfx));
            //_batches.Register(new SpriteBatcher(_gfx));
            //_batches.Register(new TilemapBatcher(_gfx, 64, 32));
        });

        builder.SetupDrawPipeline(collector =>
        {
            collector.RegisterProducerSink<IMeshDrawSink>(new MeshDrawProducer());
            collector.RegisterProducerSink<ITerrainDrawSink>(new TerrainDrawProducer());
            collector.RegisterProducer<SceneDrawProducer>(new SceneDrawProducer());
        });
        builder.SetupPassPipeline(RenderPipelineVersion.Default3D);
        _renderer.ApplyBuilder(builder);
    }

    //TODO temp solution
    private void UploadMaterialData()
    {
        Span<TextureSlotInfo> slots = stackalloc TextureSlotInfo[RenderLimits.TextureSlots];
        foreach (var material in _assets.Materials.MaterialSpan)
        {
            var length = _assets.Materials.FillTextureInfo(material!, slots);
            _assets.Materials.GetMaterialUploadData(material!, out var payload);
            _renderer.SubmitMaterialDrawData(in payload, slots.Slice(0, length));
        }
    }


    private Action? _uploadMaterialDel;

    internal void Render(float dt)
    {
        var alpha = _timeHub.Alpha;

        var frameStatus = _renderInfo.BeginRenderFrame(dt, alpha, _window, _inputSystem.InputSource,
            out var tickInfo, out var tickParams);

        _timeHub.RenderFrame(dt);

        if (_sceneManager.Current is not { } scene)
        {
            _renderer.RenderEmptyFrame(in tickInfo);
            return;
        }

        if (_renderInfo.FrameIndex > 1 && frameStatus == RenderFrameInfo.BeginFrameStatus.Resize)
        {
            _timeHub.DebounceTicker ??= new DebounceTicker(30);
        }

        var beginStatus = BeginFrameStatus.None;
        if (_timeHub.DebounceTicker?.Tick() ?? false)
        {
            _timeHub.DebounceTicker = null;
            beginStatus = BeginFrameStatus.Resize;
        }


        scene.BeforeRender(out var viewInfo);
        _renderer.BeginRenderFrame(beginStatus, in tickInfo, in tickParams, in viewInfo);

        // _renderTime.TickOrRenderEffect();

        _uploadMaterialDel ??= UploadMaterialData;
        _renderer.Render(in tickInfo, _uploadMaterialDel);

        //_renderTime.TickOrGpuDispose();
        //_renderTime.TickOrGpuUpload();

        _renderer.EndRenderFrame(out var gfxFrameResult);
        _renderInfo.EndRenderFrame(gfxFrameResult);
    }

    private void OnGpuTickDispose(int tick)
    {
    }

    private void OnGpuTickUpload(int tick)
    {
    }

    internal void Update(float dt)
    {
        _updateInfo.BeginUpdateFrame(dt, _window.WindowSize, _window.OutputSize);

        if (_stateMachine.Current != EngineStateLevel.Running)
        {
            RunSetupStateMachine();
            return;
        }

        _timeHub.UpdateFrame(dt);

        var updateInfo = _updateInfo.UpdateTickInfo;
        _sceneManager.Current?.Update(in updateInfo, _window.OutputSize);

        UpdateSceneTransitionIfNeeded();
    }

    private void GameTickUpdate(int tick)
    {
        _updateInfo.UpdateTick(tick);
        _renderer.BeginTick(_updateInfo.UpdateTickInfo);
        _inputSystem.Update();
        _sceneManager.Current?.UpdateTick(tick);
        _renderer.EndTick();
    }

    private void FpsTickUpdate(int tick)
    {
        var updateInfo = _updateInfo.UpdateTickInfo;
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