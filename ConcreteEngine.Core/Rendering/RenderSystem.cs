#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Extensions;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using static ConcreteEngine.Core.Rendering.RenderConsts;

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
}

public sealed class RenderSystem : IRenderSystem
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;
    
    private DrawCommandCollector _commandCollector  = null!;
    private RenderPipeline _commandSubmitter  = null!;
    private DrawProcessor _drawProcessor  = null!;
    private UniformBinder _uniformBinder  = null!;

    private readonly BatcherRegistry _batches = new();

    private MaterialStore _materialStore = null!;

    private IRender _render;
    private SceneDrawProducer _sceneDrawProducer = null!;
    private CommandProducerContext cmdProducerCtx = null!;

    private bool _initialized = false;

    public ICamera Camera => _render.Camera;

    internal RenderSystem(IGraphicsDevice graphics)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        
    }

    internal void InitializeGraphics()
    {
        var builder = _graphics.CreateBuilder();
        builder.RegisterUbo<FrameUniformGpuData>(UniformGpuSlot.Frame, UboDefaultCapacity.Lower);
        builder.RegisterUbo<CameraUniformGpuData>(UniformGpuSlot.Camera, UboDefaultCapacity.Lower);
        builder.RegisterUbo<DirLightUniformGpuData>(UniformGpuSlot.DirLight, UboDefaultCapacity.Lower);
        builder.RegisterUbo<MaterialUniformGpuData>(UniformGpuSlot.Material, UboDefaultCapacity.Medium);
        builder.RegisterUbo<DrawObjectUniformGpuData>(UniformGpuSlot.DrawObject, UboDefaultCapacity.Upper);
        _graphics.BuildResources(builder);

    }

    internal void Initialize(MaterialStore materialStore)
    {

        _materialStore = materialStore;
        _uniformBinder = new UniformBinder(_graphics);
        _drawProcessor = new DrawProcessor(_graphics, _materialStore, _uniformBinder);
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
        _uniformBinder.Initialize();
        
        _initialized =  true;
    }


    internal void RegisterScene(RenderType renderType, RenderTargetDescriptor desc)
    {
        if(!_initialized)
            throw new InvalidOperationException("Renderer is not initialized");
        
        if (renderType == RenderType.Render2D)
            _render = new Render2D(_graphics, _materialStore);
        else
            _render = new Render3D(_graphics, _materialStore, _uniformBinder);
        
        _render.RegisterRenderTargetsFrom(desc);
        _drawProcessor.Initialize(null, (Render3D)_render);
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();

    public Material CreateMaterial(string templateName)
        => _materialStore.CreateMaterialFromTemplate(templateName);

    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation)
        => _render.MutateRenderPass(targetId, in mutation);


    internal void BeginTick(in UpdateMetaInfo updateMeta) => _commandCollector.BeginTick(updateMeta);
    internal void EndTick() => _commandCollector.EndTick();

    internal void RenderBlank(in FrameMetaInfo frameCtx, out FrameRenderResult result)
    {
        _graphics.StartFrame(in frameCtx);
        _graphics.EndFrame(out result);
    }

    internal void Render(float alpha, in FrameMetaInfo frameCtx, in RenderGlobalSnapshot renderGlobals,
        out FrameRenderResult result)
    {

        if (!_initialized)
        {
            result = default;
            return;
        }
        
        if (frameCtx.ViewportSize != _render.Camera.ViewportSize)
            _render.Camera.ViewportSize = frameCtx.ViewportSize;
        

        _graphics.StartFrame(in frameCtx);
        PrepareRenderer(alpha, in renderGlobals);
        Execute(alpha, in renderGlobals);
        _graphics.EndFrame(out result);

        _commandSubmitter.Reset();
    }

    private void PrepareRenderer(float alpha, in RenderGlobalSnapshot renderGlobals)
    {
        _sceneDrawProducer.SetSceneGlobals(in renderGlobals);
        _render.Prepare(alpha, in renderGlobals);
        _drawProcessor.Prepare(in renderGlobals);
        _commandCollector.Collect(alpha, _commandSubmitter);
        _commandSubmitter.Prepare();
    }

    private void Execute(float alpha, in RenderGlobalSnapshot renderGlobals)
    {
        nuint blockSize   = (nuint)Unsafe.SizeOf<DrawObjectUniformGpuData>();
        nuint uboAlign    = (nuint)_gfx.Capabilities.UniformBufferOffsetAlignment;              
        nuint stride      = (blockSize + (uboAlign - 1)) & ~(uboAlign - 1);  
        var capacity = stride * (nuint)(_commandSubmitter.Count + 100);
        _uniformBinder.Prepare(capacity);

        while (_render.TryGetNextPasses(out var targetId, out var passes))
        {
            foreach (var pass in passes)
            {
                _gfx.SetBlendMode(pass.Blend);
                _gfx.SetDepthTest(pass.DepthTest);
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

        _gfx.BindMesh(_graphics.Primitives.FsqQuad);
        _gfx.DrawMesh();
    }

    
    public void Shutdown()
    {
    }
}