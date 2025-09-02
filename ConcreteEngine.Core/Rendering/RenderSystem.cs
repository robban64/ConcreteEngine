#region

using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Extensions;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Rendering.Batchers;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using static ConcreteEngine.Core.Rendering.RenderConsts;

#endregion

namespace ConcreteEngine.Core.Rendering;

public interface IRenderSystem : IGameEngineSystem
{
    ICamera Camera { get; }
    Material CreateMaterial(string templateName);
    void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation);
}


public sealed class RenderSystem : IRenderSystem
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;
    private readonly Camera2D _camera;

    private readonly MaterialStore _materialStore;
    private readonly RenderTargetRegistry _renderTargetRegistry;

    private readonly DrawCommandCollector _commandCollector;
    private readonly DrawCommandSubmitter _commandSubmitter;
    
    private readonly List<ICommandRenderer> _renderers = new();
    private readonly BatcherRegistry _batches = new ();

    public ICamera Camera => _camera;

    internal RenderSystem(IGraphicsDevice graphics, MaterialStore materialStore)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        _materialStore = materialStore;

        _camera = new Camera2D();

        _renderTargetRegistry = new RenderTargetRegistry(_graphics);
        _commandCollector = new DrawCommandCollector();
        _commandSubmitter = new DrawCommandSubmitter();

    }

    internal void Initialize(IGameFeatureManager features)
    {
        _batches.Register(new SpriteBatcher(_graphics));
        _batches.Register(new TilemapBatcher(_graphics, 64, 32));
        
        var cmdProducerCtx = new CommandProducerContext
        {
            Graphics = _graphics,
            DrawBatchers = _batches,
        };
        
        // Collector
        _commandCollector.AddProducer(new TilemapDrawProducer());
        _commandCollector.AddProducer(new SpriteDrawProducer());
        _commandCollector.AddProducer(new LightProducer());
        
        _commandCollector.GetProducer<TilemapDrawProducer>()
            .RegisterFeature<TilemapFeature>(features.Get<TilemapFeature>());
        
        _commandCollector.GetProducer<SpriteDrawProducer>()
            .RegisterFeature<SpriteFeature>(features.Get<SpriteFeature>());

        _commandCollector.GetProducer<LightProducer>()
            .RegisterFeature<LightFeature>(features.Get<LightFeature>());

        
        _commandCollector.AttachContext(cmdProducerCtx);
        
        _renderers.Add(new SpriteRenderer(_graphics, _camera, _materialStore));
        _renderers.Add(new LightRenderer(_graphics, _camera, _materialStore));

        _commandSubmitter.Initialize(_renderers);
        _commandSubmitter.Register<DrawCommandMesh, SpriteRenderer>(DrawCommandTag.Mesh2D, DrawCommandId.Tilemap, DrawCommandId.Sprite);
        _commandSubmitter.Register<DrawCommandLight, LightRenderer>(DrawCommandTag.Effect2D, DrawCommandId.Light);
    }

    internal void RegisterScene(RenderTargetDescription desc)
    {
        _renderTargetRegistry.RegisterRenderTargetsFrom(desc);
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
        _camera.SetViewport(frameCtx.ViewportSize);
        
        _graphics.StartFrame(in frameCtx);
        PrepareRenderer(alpha);
        Execute(alpha);
        _graphics.EndFrame(out result);

        _commandSubmitter.Reset();
    }

    private void PrepareRenderer(float alpha)
    {
        _camera.PrepareRender();
        _commandCollector.Collect(alpha, _commandSubmitter);
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