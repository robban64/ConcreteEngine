#region

using System.Diagnostics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering;

public enum RenderType
{
    Render2D,
    Render3D
}

public interface IRenderSystem : IGameEngineSystem
{
    ICamera Camera { get; }

    TSink GetSink<TSink>() where TSink : IDrawSink;
    Material CreateMaterial(string templateName);
    void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation);

    public SceneRenderProperties SceneRenderProps { get; }
}

public sealed class RenderSystem : IRenderSystem
{
    private readonly IGraphicsRuntime _graphics;
    private readonly GfxContext _gfx;
    private readonly GfxCommands _gfxCmd;
    
    private readonly RenderRegistry _renderRegistry;

    private DrawCommandCollector _commandCollector = null!;
    private RenderPipeline _commandSubmitter = null!;
    private DrawProcessor _drawProcessor = null!;

    private readonly BatcherRegistry _batches = new();

    private MaterialStore _materialStore = null!;

    private IRender _render;
    private SceneDrawProducer _sceneDrawProducer = null!;
    private CommandProducerContext _cmdProducerCtx = null!;

    private bool _initialized = false;

    private FrameInfo _frameCtx;

    private RenderGlobalSnapshot _snapshot;

    public SceneRenderProperties SceneRenderProps { get; }

    public ICamera Camera => _render.Camera;

    internal RenderSystem(IGraphicsRuntime graphics, Size2D outputSize)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        _gfxCmd = graphics.Gfx.Commands;
        SceneRenderProps = new SceneRenderProperties();
        SceneRenderProps.SetOutputSize(outputSize);
        SceneRenderProps.Commit();
        _snapshot = SceneRenderProps.CurrentSnapshot;
    }

    internal void InitializeGraphics()
    {
        InvalidOpThrower.ThrowIf(_snapshot.OutputSize.Width <= 1);
        InvalidOpThrower.ThrowIf(_snapshot.OutputSize.Height <= 1);

        _renderRegistry.BeginRegistration(_snapshot.OutputSize);
        _renderRegistry.RegisterUniformBuffer<FrameUniformGpuData>();
        _renderRegistry.RegisterUniformBuffer<CameraUniformGpuData>();
        _renderRegistry.RegisterUniformBuffer<DirLightUniformGpuData>();
        _renderRegistry.RegisterUniformBuffer<MaterialUniformGpuData>();
        _renderRegistry.RegisterUniformBuffer<DrawObjectUniformGpuData>();
        _renderRegistry.RegisterUniformBuffer<FramePostProcessUniform>();
        
        
    }

    internal void Initialize(MaterialStore materialStore)
    {
        _materialStore = materialStore;
        _drawProcessor = new DrawProcessor(_gfx, _materialStore);

        _commandCollector = new DrawCommandCollector();
        _commandSubmitter = new RenderPipeline(_drawProcessor);

        _batches.Register(new TerrainBatcher(_gfx));
        _batches.Register(new SpriteBatcher(_gfx));
        _batches.Register(new TilemapBatcher(_gfx, 64, 32));

        _cmdProducerCtx = new CommandProducerContext { Gfx = _gfx, DrawBatchers = _batches, };

        // Collector
        _commandCollector.RegisterProducerSink<IMeshDrawSink>(new MeshDrawProducer());
        _commandCollector.RegisterProducerSink<ITerrainDrawSink>(new TerrainDrawProducer());
        _sceneDrawProducer = new SceneDrawProducer();
        _commandCollector.RegisterProducer<SceneDrawProducer>(_sceneDrawProducer);


        _commandCollector.AttachContext(_cmdProducerCtx);
        _commandSubmitter.Initialize();
        _commandCollector.InitializeProducers();
        _drawProcessor.Initialize();

        _initialized = true;
    }


    internal void RegisterScene(in Vector2D<int> outputSize, RenderType renderType, RenderTargetDescriptor desc)
    {
        if (!_initialized)
            throw new InvalidOperationException("Renderer is not initialized");

        SceneRenderProps.Commit();
        if (renderType == RenderType.Render2D)
            _render = new Render2D(_gfx, _materialStore, in _snapshot);
        else
            _render = new Render3D(_gfx, _drawProcessor, in _snapshot);

        _render.RegisterRenderTargetsFrom(in outputSize, desc);
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();

    public Material CreateMaterial(string templateName) => _materialStore.CreateMaterialFromTemplate(templateName);

    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation) =>
        _render.MutateRenderPass(targetId, in mutation);


    internal void BeginTick(in UpdateInfo update) => _commandCollector.BeginTick(update);
    internal void EndTick() => _commandCollector.EndTick();

    internal void Render(float alpha, in FrameInfo frameCtx)
    {
        Debug.Assert(_initialized);
        _frameCtx = frameCtx;
        if (frameCtx.Viewport != _render.Camera.Viewport)
            _render.Camera.Viewport = frameCtx.Viewport;

        SceneRenderProps.SetOutputSize(frameCtx.OutputSize.ToSize2D());
        SceneRenderProps.Commit();
        _snapshot = SceneRenderProps.CurrentSnapshot;


        PrepareRenderer(alpha, in frameCtx);
        Execute(alpha);
        _commandSubmitter.Reset();
    }

    private void PrepareRenderer(float alpha, in FrameInfo frameCtx)
    {
        _sceneDrawProducer.SetSceneGlobals(in _snapshot);
        _render.Prepare(alpha, in frameCtx, in _snapshot);
        _commandCollector.Collect(alpha, in _snapshot, _commandSubmitter);
        _commandSubmitter.Prepare();
    }

    private void Execute(float alpha)
    {
        var capacity =
            UniformBufferUtils.GetCapacityForEntities<DrawObjectUniformGpuData>(_commandSubmitter.Count + 100);
        _drawProcessor.Prepare(in _snapshot, capacity);

        _commandSubmitter.DrainTransformQueue();

        while (_render.TryGetNextPasses(out var targetId, out var passes))
        {
            foreach (var pass in passes)
            {
                ExecutePass(targetId, pass);
            }
        }
    }

    private void ExecutePass(RenderTargetId target, IRenderPassDescriptor pass)
    {
        ArgumentNullException.ThrowIfNull(pass);

        if (pass is BlitRenderPass blitPass)
        {
            _gfxCmd.BlitFramebuffer(blitPass.BlitFbo, blitPass.TargetFbo, blitPass.LinearFilter);
            return;
        }

        var isScreenPass = pass.TargetFbo == default;
        
        //if(pass.DepthTest) _gfxCmd.SetDepthMode(DepthMode.Lequal);

        var isScenePass = pass is IScenePass;

         if (pass.TargetFbo == default)
            _gfxCmd.BeginScreenPass(pass.Clear);
        else
            _gfxCmd.BeginRenderPass(pass.TargetFbo, pass.Clear, isScenePass);

      
        switch (pass)
        {
            case IScenePass scePass:
                _render.RenderScenePass(scePass, _commandSubmitter);
                break;
            case PostEffectPass pPass:
                _render.RenderPostEffectPass(pPass);
                break;
            case ScreenPass scrPass: 
                _render.RenderScreenPass(scrPass);
                break;
            case IDepthPass dPass:
                _render.RenderDepthPass(dPass, _commandSubmitter);
                break;

        }

        if (pass.Op == RenderPassOp.DrawScene)
        {
            _gfxCmd.EndRenderPass();
            return;
        }
        
        if (!isScreenPass)
            _gfxCmd.EndRenderPass();

        if (pass is PostEffectPass postEffectPass && postEffectPass.GenerateMipMapAfter)
        {
            _gfx.Textures.GenerateMipMaps(postEffectPass.OutputTexture);
        }
    }

    public void Shutdown()
    {
    }
}