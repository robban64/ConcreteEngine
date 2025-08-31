#region

using ConcreteEngine.Common.Extensions;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;
using static ConcreteEngine.Core.Rendering.RenderConsts;

#endregion

namespace ConcreteEngine.Core.Rendering;

public interface IRenderSystem : IGameEngineSystem
{
    Material CreateMaterial(string templateName);
    void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation);
}

public sealed class RenderSystem : IRenderSystem
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;
    private readonly GameCamera _camera;

    private readonly MaterialStore _materialStore;
    private readonly RenderTargetRegistry _renderTargetRegistry;

    private readonly DrawCommandCollector _commandCollector;
    private readonly DrawCommandSubmitter _commandSubmitter;
    private readonly CommandProducerContext _commandProducerContext;


    private readonly SpriteRenderer _spriteRenderer;
    private readonly LightRenderer _lightRenderer;

    private readonly SpriteBatcher _spriteBatch;
    private readonly TilemapBatcher _tilemapBatcher;

    public SpriteBatcher SpriteBatch => _spriteBatch;
    public TilemapBatcher TilemapBatch => _tilemapBatcher;


    internal RenderSystem(IGraphicsDevice graphics, GameCamera camera, MaterialStore materialStore)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        _camera = camera;

        _materialStore = materialStore;

        _renderTargetRegistry = new RenderTargetRegistry(_graphics);

        _commandCollector = new DrawCommandCollector();

        _spriteRenderer = new SpriteRenderer(_graphics, _camera, _materialStore);
        _lightRenderer = new LightRenderer(_graphics, _camera, _materialStore);
        _commandSubmitter = new DrawCommandSubmitter([_spriteRenderer, _lightRenderer]);

        _spriteBatch = new SpriteBatcher(graphics);
        _tilemapBatcher = new TilemapBatcher(graphics, 64, 32);

        _commandProducerContext = new CommandProducerContext
        {
            Graphics = _graphics,
            SpriteBatch = _spriteBatch,
            TilemapBatch = _tilemapBatcher
        };
    }

    internal void Initialize(GameSceneConfigBuilder builder)
    {
        // Command Collector
        foreach (var (order, producer) in builder.DrawProducers)
            _commandCollector.AddProducer(order, producer());

        _commandCollector.Initialize(_commandProducerContext);

        // Renderers
        foreach (var registry in builder.Renderers)
        {
            foreach (var cmdId in registry.CommandIds)
            {
                registry.Bind(_commandSubmitter, cmdId, registry.CommandTag);
            }
        }
        
        // RenderPasses
        _renderTargetRegistry.RegisterRenderTargetsFrom(builder.RenderTargetsDesc);
        
        
    }

    public void RegisterDrawFeature(int order, IDrawableFeature feature, Type producerType)
    {
        var producer = _commandCollector.GetProducer(producerType);
        producer.RegisterFeature(order, feature);
    }

    public void RegisterDrawFeature<TProducer, TFeature, TDrawData>(int order, TFeature feature)
        where TProducer : DrawCommandProducer<TDrawData>
        where TFeature : class, IGameFeature, IDrawableFeature<TDrawData>
        where TDrawData : class
    {
        var producer = _commandCollector.GetProducer<TProducer, TDrawData>();
        producer.RegisterFeature<TFeature>(order, feature);
    }


    public void RegisterRenderer<TCommand, TRenderer>(DrawCommandId id, DrawCommandTag tag)
        where TCommand : struct, IDrawCommand
        where TRenderer : class, ICommandRenderer<TCommand>
    {
        _commandSubmitter.Register<TCommand, TRenderer>(id, tag);
    }

    public Material CreateMaterial(string templateName)
        => _materialStore.CreateMaterialFromTemplate(templateName);

    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation) 
        => _renderTargetRegistry.MutateRenderPass(targetId, in mutation);
    
    public void Shutdown()
    {
    }

    internal void Render(float alpha, in FrameMetaInfo frameCtx, out FrameRenderResult result)
    {
        _commandProducerContext.Alpha = alpha;
        _graphics.StartFrame(in frameCtx);
        PrepareRenderer();
        Execute(alpha);
        _graphics.EndFrame(out result);

        _commandSubmitter.Reset();
    }

    private void PrepareRenderer()
    {
        _camera.PrepareRender();
        _commandCollector.Collect(_commandProducerContext, _commandSubmitter);
        _commandSubmitter.Prepare();

        var projectionViewMatrix = _camera.RenderTransform.ProjectionViewMatrix;
        foreach (var material in _materialStore.Materials)
        {
            if (material.HasViewProjection)
            {
                _gfx.UseShader(material.ShaderId);
                _gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, in projectionViewMatrix);
            }
        }
    }

    private void Execute(float alpha)
    {
        foreach (var (renderTarget, passes) in _renderTargetRegistry)
        {
            foreach (var pass in passes)
            {
                //var (prevBlend, prevDepthTest) = (_gfx.BlendMode, _gfx.DepthTest);
                _gfx.SetBlendMode(pass.Blend);
                _gfx.SetDepthTest(pass.DepthTest);

                ExecutePass(renderTarget, pass);

                //_gfx.SetBlendMode(prevBlend);
                //_gfx.SetDepthTest(prevDepthTest);
            }
        }
        
        /*
        for (int target = 0; target < RenderTargetCount; target++)
        {
            var renderTarget = (RenderTargetId)target;
            var passList = renderPasses[target];

            foreach (var pass in passList)
            {
                //var (prevBlend, prevDepthTest) = (_gfx.BlendMode, _gfx.DepthTest);
                _gfx.SetBlendMode(pass.Blend);
                _gfx.SetDepthTest(pass.DepthTest);

                ExecutePass(renderTarget, pass);

                //_gfx.SetBlendMode(prevBlend);
                //_gfx.SetDepthTest(prevDepthTest);
            }
        }*/
    }

    private void ExecutePass(RenderTargetId target, IRenderPass pass)
    {
        if (pass.Op == RenderPassOp.Blit && pass is BlitRenderPass blitPass)
        {
            // preserves bindings internally
            _gfx.BlitFramebuffer(blitPass.BlitFbo, blitPass.TargetFbo, blitPass.LinearFilter);
            return;
        }

        var isScreenPass = pass.TargetFbo == default;

        if (pass.TargetFbo == default)
            _gfx.BeginScreenPass(pass.Clear?.ClearColor, pass.Clear?.ClearMask);
        else
            _gfx.BeginRenderPass(pass.TargetFbo, pass.Clear?.ClearColor, pass.Clear?.ClearMask);

        if (pass.Op == RenderPassOp.DrawScene)
        {
            if (pass is SceneRenderPass scenePass)
                RenderScenePass(scenePass);

            if (pass is LightRenderPass lightPass)
                RenderLightPass(lightPass);

            _gfx.EndRenderPass();
            return;
        }


        if (pass.Op == RenderPassOp.FullscreenQuad && pass is FsqRenderPass fsqPass)
        {
            DrawFullscreenQuad(fsqPass);
        }

        if (!isScreenPass)
        {
            _gfx.EndRenderPass();
        }
    }


    private void RenderScenePass(SceneRenderPass scenePass)
    {
        _commandSubmitter.DrainCommandQueue(RenderTargetId.Scene);
    }

    private void RenderLightPass(LightRenderPass lightPass)
    {
        _gfx.UseShader(lightPass.Shader);
        _commandSubmitter.DrainCommandQueue(RenderTargetId.SceneLight);
    }

    private void DrawFullscreenQuad(FsqRenderPass pass)
    {
        ArgumentNullException.ThrowIfNull(pass);
        ArgumentNullException.ThrowIfNull(pass.SourceTextures);
        ArgumentOutOfRangeException.ThrowIfZero(pass.SourceTextures.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pass.SourceTextures.Length, 4, nameof(pass.SourceTextures));

        var viewport = _camera.RenderTransform.ViewportSize;
        _gfx.UseShader(pass.Shader);
        _gfx.SetUniform(ShaderUniform.TexelSize, viewport.ToSystemVec2() * pass.SizeRatio);

        for (int i = 0; i < pass.SourceTextures.Length; i++)
        {
            _gfx.BindTexture(pass.SourceTextures[i], (uint)i);
        }

        _gfx.BindMesh(_graphics.QuadMeshId);
        _gfx.DrawMesh();
    }
}