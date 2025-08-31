#region

using ConcreteEngine.Common.Extensions;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Rendering.Batchers;
using ConcreteEngine.Core.Rendering.Emitters;
using ConcreteEngine.Core.Rendering.Pipeline;
using ConcreteEngine.Core.Rendering.Renderers;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using static ConcreteEngine.Core.Rendering.RenderConsts;

#endregion

namespace ConcreteEngine.Core.Rendering;

public interface IRenderSystem : IGameEngineSystem
{
    SpriteBatcher SpriteBatch { get; }
    TilemapBatcher TilemapBatch { get; }

    Material CreateMaterial(string templateName);
}

public sealed class RenderSystem : IRenderSystem
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;
    private readonly GameCamera _camera;

    private readonly MaterialStore _materialStore;
    private readonly List<IRenderPass>[] _renderPassDesc;

    private readonly DrawEmitterCollector _emitterCollector;
    private readonly DrawCommandSubmitter _commandSubmitter;
    private readonly DrawEmitterContext _emitterContext;

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

        _renderPassDesc = new List<IRenderPass>[RenderTargetCount];
        for (int i = 0; i < RenderTargetCount; i++)
        {
            _renderPassDesc[i] = new List<IRenderPass>(4);
        }

        _emitterCollector = new DrawEmitterCollector();

        _spriteRenderer = new SpriteRenderer(_graphics, _camera, _materialStore);
        _lightRenderer = new LightRenderer(_graphics, _camera, _materialStore);
        _commandSubmitter = new DrawCommandSubmitter([_spriteRenderer, _lightRenderer]);

        _spriteBatch = new SpriteBatcher(graphics);
        _tilemapBatcher = new TilemapBatcher(graphics, 64, 32);

        _emitterContext = new DrawEmitterContext
        {
            Graphics = _graphics,
            SpriteBatch = _spriteBatch,
            TilemapBatch = _tilemapBatcher
        };
    }

    internal void Initialize(GameSceneConfigBuilder builder)
    {
        foreach (var (order, emitter) in builder.Emitters)
            _emitterCollector.AddEmitter(order, emitter());

        _emitterCollector.Initialize();

        foreach (var pass in builder.Passes.Values)
            RegisterRenderPass(pass.Target, pass.Pass);

        foreach (var registry in builder.Renderers)
        {
            foreach (var cmdId in registry.CommandIds)
            {
                registry.Bind(_commandSubmitter, cmdId, registry.CommandTag);
            }
        }
    }

    public void RegisterDrawFeature(int order, IDrawableFeature feature, Type emitterType)
    {
        var emitter = _emitterCollector.GetEmitter(emitterType);
        emitter.RegisterFeature(order, feature);
    }

    public void RegisterDrawFeature<TEmitter, TFeature, TDrawData>(int order, TFeature feature)
        where TEmitter : DrawCommandEmitter<TDrawData>
        where TFeature : class, IGameFeature, IDrawableFeature<TDrawData>
        where TDrawData : class
    {
        var emitter = _emitterCollector.GetEmitter<TEmitter, TDrawData>();
        emitter.RegisterFeature<TFeature>(order, feature);
    }

    public void RegisterRenderPass(RenderTargetId target, IRenderPass pass)
    {
        if (pass.Op == RenderPassOp.FullscreenQuad && pass is not FsqRenderPass)
            throw new InvalidOperationException($"RenderPass: FullscreenQuad require {nameof(FsqRenderPass)}");

        if (pass.Op == RenderPassOp.Blit && pass is not BlitRenderPass)
            throw new InvalidOperationException($"RenderPass: Blit require {nameof(BlitRenderPass)}");


        _renderPassDesc[(int)target].Add(pass);
    }

    public void RegisterRenderer<TCommand, TRenderer>(DrawCommandId id, DrawCommandTag tag)
        where TCommand : struct, IDrawCommand
        where TRenderer : class, ICommandRenderer<TCommand>
    {
        _commandSubmitter.Register<TCommand, TRenderer>(id, tag);
    }

    public Material CreateMaterial(string templateName)
        => _materialStore.CreateMaterialFromTemplate(templateName);


    public void Shutdown()
    {
    }

    internal void Render(float alpha, in FrameMetaInfo frameCtx, out FrameRenderResult result)
    {
        _emitterContext.Alpha = alpha;
        _graphics.StartFrame(in frameCtx);
        PrepareRenderer();
        Execute(alpha);
        _graphics.EndFrame(out result);

        _commandSubmitter.Reset();
    }

    private void PrepareRenderer()
    {
        _camera.PrepareRender();
        _emitterCollector.Collect(_emitterContext, _commandSubmitter);
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
        for (int target = 0; target < RenderTargetCount; target++)
        {
            var renderTarget = (RenderTargetId)target;
            var passList = _renderPassDesc[target];

            foreach (var pass in passList)
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