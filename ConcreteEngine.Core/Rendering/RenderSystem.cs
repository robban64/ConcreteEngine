#region

using System.Diagnostics;
using ConcreteEngine.Common.Extensions;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Graphics;
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
    
    public SceneRenderGlobals RenderGlobals { get; }
}

public sealed class RenderSystem : IRenderSystem
{
    private readonly IGraphicsRuntime _graphics;
    private readonly IGraphicsContext _gfx;
    
    private DrawCommandCollector _commandCollector  = null!;
    private RenderPipeline _commandSubmitter  = null!;
    private DrawProcessor _drawProcessor  = null!;

    private readonly BatcherRegistry _batches = new();

    private MaterialStore _materialStore = null!;

    private IRender _render;
    private SceneDrawProducer _sceneDrawProducer = null!;
    private CommandProducerContext cmdProducerCtx = null!;

    private bool _initialized = false;

    private FrameInfo _frameCtx;
    
    private RenderGlobalSnapshot _snapshot;
    
    public SceneRenderGlobals RenderGlobals { get; }

    public ICamera Camera => _render.Camera;

    internal RenderSystem(IGraphicsRuntime graphics, in Vector2D<int> outputSize)
    {
        _graphics = graphics;
        _gfx = graphics.Context;
        RenderGlobals = new SceneRenderGlobals();
        RenderGlobals.SetOutputSize(in outputSize);
        RenderGlobals.Commit();
        _snapshot = RenderGlobals.Snapshot;

    }

    internal void InitializeGraphics()
    {
        _graphics.Allocator.CreateUniformBuffer<FrameUniformGpuData>(UniformGpuSlot.Frame, UboDefaultCapacity.Lower, out _);
        _graphics.Allocator.CreateUniformBuffer<CameraUniformGpuData>(UniformGpuSlot.Camera, UboDefaultCapacity.Lower, out _);
        _graphics.Allocator.CreateUniformBuffer<DirLightUniformGpuData>(UniformGpuSlot.DirLight, UboDefaultCapacity.Lower, out _);
        _graphics.Allocator.CreateUniformBuffer<MaterialUniformGpuData>(UniformGpuSlot.Material, UboDefaultCapacity.Medium, out _);
        _graphics.Allocator.CreateUniformBuffer<DrawObjectUniformGpuData>(UniformGpuSlot.DrawObject, UboDefaultCapacity.Upper, out _);
    }

    internal void Initialize(MaterialStore materialStore)
    {

        _materialStore = materialStore;
        _drawProcessor = new DrawProcessor(_gfx, _graphics.Repository, _materialStore);

        _commandCollector = new DrawCommandCollector();
        _commandSubmitter = new RenderPipeline(_drawProcessor);

        _batches.Register(new TerrainBatcher(_graphics));
        _batches.Register(new SpriteBatcher(_graphics));
        _batches.Register(new TilemapBatcher(_graphics, 64, 32));

        cmdProducerCtx = new CommandProducerContext
        {
            Graphics = _graphics,
            DrawBatchers = _batches,
        };

        // Collector
        _commandCollector.RegisterProducerSink<IMeshDrawSink>(new MeshDrawProducer());
        _commandCollector.RegisterProducerSink<ITerrainDrawSink>(new TerrainDrawProducer());
        _sceneDrawProducer = new  SceneDrawProducer();
        _commandCollector.RegisterProducer<SceneDrawProducer>(_sceneDrawProducer);


        _commandCollector.AttachContext(cmdProducerCtx);
        _commandSubmitter.Initialize();
        _commandCollector.InitializeProducers();
        _drawProcessor.Initialize();
        
        _initialized =  true;
    }


    internal void RegisterScene(in Vector2D<int> outputSize, RenderType renderType, RenderTargetDescriptor desc)
    {
        if(!_initialized)
            throw new InvalidOperationException("Renderer is not initialized");
        
        RenderGlobals.Commit();
        if (renderType == RenderType.Render2D)
            _render = new Render2D(_graphics, _materialStore, in _snapshot);
        else
            _render = new Render3D(_graphics, _drawProcessor, in _snapshot);
        
        _render.RegisterRenderTargetsFrom(in outputSize, desc);
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();

    public Material CreateMaterial(string templateName)
        => _materialStore.CreateMaterialFromTemplate(templateName);

    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation)
        => _render.MutateRenderPass(targetId, in mutation);


    internal void BeginTick(in UpdateInfo update) => _commandCollector.BeginTick(update);
    internal void EndTick() => _commandCollector.EndTick();

    internal void Render(float alpha, in FrameInfo frameCtx)
    {
        Debug.Assert(_initialized);
        _frameCtx = frameCtx;
        if (frameCtx.Viewport != _render.Camera.ViewportSize)
            _render.Camera.ViewportSize = frameCtx.Viewport;
        
        RenderGlobals.SetOutputSize(frameCtx.OutputSize);
        RenderGlobals.Commit();
        _snapshot = RenderGlobals.Snapshot;

        
        PrepareRenderer(alpha);
        Execute(alpha);
        _commandSubmitter.Reset();
    }

    private void PrepareRenderer(float alpha)
    {
        _sceneDrawProducer.SetSceneGlobals(in _snapshot);
        _render.Prepare(alpha, in _snapshot);
        _commandCollector.Collect(alpha, in _snapshot, _commandSubmitter);
        _commandSubmitter.Prepare();
    }

    private void Execute(float alpha)
    {
        var capacity = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniformGpuData>(_commandSubmitter.Count + 100);
        _drawProcessor.Prepare(in _snapshot, capacity);

        _commandSubmitter.DrainTransformQueue();

        while (_render.TryGetNextPasses(out var targetId, out var passes))
        {
            foreach (var pass in passes)
            {
                _gfx.SetBlendMode(pass.Blend);
                _gfx.SetDepthMode(pass.DepthTest ? DepthMode.WriteLequal : DepthMode.Disabled);
                ExecutePass(targetId, pass);
            }

        }
    }

    private void ExecutePass(RenderTargetId target, IRenderPassDescriptor pass)
    {
        if (pass is BlitRenderPass blitPass)
        {
            _gfx.BlitFramebuffer(blitPass.BlitFbo, blitPass.TargetFbo, blitPass.LinearFilter);
            return;
        }

        var isScreenPass = pass.TargetFbo == default;

        if (pass.TargetFbo == default)
            _gfx.BeginScreenPass(pass.Clear?.ClearColor, pass.Clear?.ClearMask);
        else
            _gfx.BeginRenderPass(pass.TargetFbo, pass.Clear?.ClearColor, pass.Clear?.ClearMask);

        
        switch (pass)
        {
            case IScenePass scenePass:
                _render.RenderScenePass(scenePass, _commandSubmitter); // handles Scene/Light via runtime type
                break;
            case IFsqPass fsq:
                DrawFullscreenQuad(fsq);
                break;
            case IDepthPass depthPass:
                _render.RenderDepthPass(depthPass, _commandSubmitter);
                break;
        }
        
        if (pass.Op == RenderPassOp.DrawScene)
        {

            _gfx.EndRenderPass();
            return;
        }

        if (pass.Op == RenderPassOp.FullscreenQuad && pass is IFsqPass fsqPass)
        {
            DrawFullscreenQuad(fsqPass);
        }

        if (!isScreenPass)
        {
            _gfx.EndRenderPass();
        }
    }

    private void DrawFullscreenQuad(IFsqPass pass)
    {
        ArgumentNullException.ThrowIfNull(pass);
        ArgumentNullException.ThrowIfNull(pass.SourceTextures);
        ArgumentOutOfRangeException.ThrowIfZero(pass.SourceTextures.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pass.SourceTextures.Length, 4, nameof(pass.SourceTextures));

        var viewport = _render.Camera.ViewportSize;
        _gfx.UseShader(pass.Shader);
        _gfx.SetUniform(ShaderUniform.TexelSize, viewport.ConvertToVec2() * pass.SizeRatio);

        for (int i = 0; i < pass.SourceTextures.Length; i++)
        {
            _gfx.BindTexture(pass.SourceTextures[i], (uint)i);
        }

        _gfx.BindMesh(_graphics.FactoryHub.Primitives.FsqQuad);
        _gfx.DrawBoundMesh();
    }

    
    public void Shutdown()
    {
    }
}