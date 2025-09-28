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
    void MutateRenderPass(RenderTargetId targetId);

    public SceneRenderProperties SceneRenderProps { get; }
}

public sealed class RenderSystem : IRenderSystem
{
    private readonly IGraphicsRuntime _graphics;
    private readonly GfxContext _gfx;
    private readonly GfxCommands _gfxCmd;

    private RenderRegistry _renderRegistry;
    private RenderPassRegistry _renderPassRegistry;

    private DrawCommandCollector _commandCollector = null!;
    private RenderPipeline _commandSubmitter = null!;
    private DrawProcessor _drawProcessor = null!;

    private readonly BatcherRegistry _batches = new();

    private MaterialStore _materialStore = null!;

    private SceneDrawProducer _sceneDrawProducer = null!;
    private CommandProducerContext _cmdProducerCtx = null!;

    private bool _initialized = false;

    private FrameInfo _frameCtx;

    private RenderGlobalSnapshot _snapshot;

    public SceneRenderProperties SceneRenderProps { get; }

    public ICamera Camera { get; } 
    

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

        _renderRegistry = new RenderRegistry(_gfx);
        _renderRegistry.BeginRegistration(_snapshot.OutputSize);
        _renderRegistry.RegisterUniformBuffer<FrameUniformGpuData>();
        _renderRegistry.RegisterUniformBuffer<CameraUniformGpuData>();
        _renderRegistry.RegisterUniformBuffer<DirLightUniformGpuData>();
        _renderRegistry.RegisterUniformBuffer<MaterialUniformGpuData>();
        _renderRegistry.RegisterUniformBuffer<DrawObjectUniformGpuData>();
        _renderRegistry.RegisterUniformBuffer<FramePostProcessUniform>();

        _renderPassRegistry = new RenderPassRegistry(new RenderCommandOps(_gfx, _drawProcessor));
        
        //NOTE! just an example showcase, this would not work.
        _renderPassRegistry.Register(RenderTargetId.Scene, new ScenePassState(default))
            .AddBeforeOp((in RenderPassCtx ctx, in ScenePassState state) =>
            {
                ctx.CmdOps.BeginRenderPass(ctx.FboId, state.ClearColor, true);
            });

        _renderPassRegistry.Register(RenderTargetId.Scene, new ResolvePassState())
            .AddBeforeOp((in RenderPassCtx ctx, in ResolvePassState state) =>
            {
                //ApplyState(_activeState with{ FramebufferSrgb = false });
                ctx.CmdOps.Blit(ctx.FboId, state.BlitFbo, true);
                
                // Something like this
                // ctx.ResolveTo(RenderTargetId.PostEffect, 0, ColorTexture)
                // return ResolveTo(RenderTargetId.PostEffect, 0, ColorTexture)
            });

        _renderPassRegistry.Register(RenderTargetId.PostEffect, new PostPassState())
            .AddBeforeOp((in RenderPassCtx ctx, in PostPassState state) =>
            {
                ctx.CmdOps.BeginRenderPass(ctx.FboId, state.ClearColor, false);
                //ctx.CmdOps.UpdateStates(state.PassState);
                //ctx.CmdOps.ClearColor(state.ClearColor);
                
            }).AddAfterOp((in RenderPassCtx ctx, in PostPassState state) =>
            {
                if (ctx.Pass == 0) ctx.CmdOps.GenerateMips(state.OutputTextureId);
            });

        _renderPassRegistry.Register(RenderTargetId.PostEffect, new PostPassState())
            .AddBeforeOp((in RenderPassCtx ctx, in PostPassState state) =>
            {
                ctx.CmdOps.BeginRenderPass(ctx.FboId, state.ClearColor, false);
                //ctx.CmdOps.UpdateStates(state.PassState);
                //ctx.CmdOps.ClearColor(state.ClearColor);

            });

        _renderPassRegistry.Register(RenderTargetId.Screen, new PostPassState(default, default, default))
            .AddBeforeOp((in RenderPassCtx ctx, in PostPassState state) =>
            {
                ctx.CmdOps.BeginScreenPass(state.ClearColor);
                //...
            });
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
        //_render.RegisterRenderTargetsFrom(in outputSize, desc);
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();

    public Material CreateMaterial(string templateName) => _materialStore.CreateMaterialFromTemplate(templateName);

    public void MutateRenderPass(RenderTargetId targetId) {return;}
        //_render.MutateRenderPass(targetId);


    internal void BeginTick(in UpdateInfo update) => _commandCollector.BeginTick(update);
    internal void EndTick() => _commandCollector.EndTick();

    internal void Render(float alpha, in FrameInfo frameCtx)
    {
        Debug.Assert(_initialized);
        _frameCtx = frameCtx;
       // if (frameCtx.Viewport != _render.Camera.Viewport)
            //_render.Camera.Viewport = frameCtx.Viewport;

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
        //_render.Prepare(alpha, in frameCtx, in _snapshot);
        _commandCollector.Collect(alpha, in _snapshot, _commandSubmitter);
        _commandSubmitter.Prepare();
    }

    private void Execute(float alpha)
    {
        var capacity =
            UniformBufferUtils.GetCapacityForEntities<DrawObjectUniformGpuData>(_commandSubmitter.Count + 100);
        _drawProcessor.Prepare(in _snapshot, capacity);

        _commandSubmitter.DrainTransformQueue();

        while (_renderPassRegistry.TryGetNextPasses(out var target, out var passes))
        {
            foreach (var pass in passes)
            {
                ExecutePass(in target, in pass);
            }
        }
    }
    private void ExecutePass(in RenderTarget target, in IRenderPassEntry pass)
    {
        Debug.Assert(target != null &&  pass != null);
        pass.ExecuteBefore(in _renderPassRegistry.Ctx);

        if(target.TargetId == RenderTargetId.Scene && pass.Index == 0)
            _commandSubmitter.DrainCommandQueue(RenderTargetId.Scene);

        _gfxCmd.EndRenderPass();
        pass.ExecuteAfter(in  _renderPassRegistry.Ctx);
/*
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
        */
    }

    public void Shutdown()
    {
    }
}