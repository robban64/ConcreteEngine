#region

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Extensions;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Rendering.Batchers.Sprite;
using ConcreteEngine.Core.Rendering.Batchers.Tilemap;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;
using static ConcreteEngine.Core.Rendering.RenderConsts;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderSystem : IGameEngineSystem
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;
    private readonly ViewTransform2D _camera;

    private readonly MaterialStore _materialStore;
    private readonly List<IRenderPass>[] _renderPassDesc;

    private readonly DrawEmitterCollector _emitterCollector;
    private readonly DrawCommandSubmitter _commandSubmitter;
    private readonly DrawEmitterContext _emitterContext;
    
    private readonly CommandRenderer  _commandRenderer;

    private readonly SpriteBatcher _spriteBatch;
    private readonly TilemapBatcher _tilemapBatcher;

    public SpriteBatcher SpriteBatch => _spriteBatch;

    internal RenderSystem(IGraphicsDevice graphics, ViewTransform2D camera, MaterialStore materialStore)
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
        _commandSubmitter = new DrawCommandSubmitter();

        _commandRenderer = new CommandRenderer(_graphics, _camera, _materialStore);

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
            RegisterRenderPass(pass.Target,  pass.Pass);

        foreach (var cmd in builder.Commands)
            cmd.Bind(_commandSubmitter, cmd.CommandId);
    }

    public void RegisterDrawFeature(int order, IDrawableFeature feature, Type emitterType)
    {
        var emitter = _emitterCollector.GetEmitter(emitterType);
        emitter.RegisterFeature(order, feature);
    }

    public void RegisterDrawFeature<TEmitter, TFeature, TEntity>(int order, TFeature feature)
        where TEmitter : DrawCommandEmitter<TEntity>
        where TFeature : class, IGameFeature, IDrawableFeature<TEntity>
        where TEntity : struct
    {
        var emitter = _emitterCollector.GetEmitter<TEmitter, TEntity>();
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

    public void RegisterCommand<T>(DrawCommandId commandId) where T : struct, IDrawCommand
    {
        _commandSubmitter.Register<T>(commandId);
    }

    public Material CreateMaterialFromTemplate(string templateName)
        => _materialStore.CreateMaterialFromTemplate(templateName);


    internal void Render(float alpha, in GraphicsFrameContext frameCtx)
    {
        _emitterContext.Alpha = alpha;
        _graphics.StartFrame(in frameCtx);
        PrepareRenderer();
        Execute(alpha);
        _graphics.EndFrame();
    }

    private void PrepareRenderer()
    {
        _commandSubmitter.Reset();
        _emitterCollector.Collect(_emitterContext, _commandSubmitter);

        var projectionViewMatrix = _camera.ProjectionViewMatrix;
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
            _commandRenderer.DrawFullscreenQuad(fsqPass);
        }

        if (!isScreenPass)
        {
            _gfx.EndRenderPass();
        }
    }


    private void RenderScenePass(SceneRenderPass scenePass)
    {
        var projView = _camera.ProjectionViewMatrix;
        var target = RenderTargetId.Scene;
        var commands = _commandSubmitter.DrainCommandQueue<DrawCommandMesh>();
        _commandRenderer.DrawMeshCommands(commands);

    }

    private void RenderLightPass(LightRenderPass lightPass)
    {
        var projView = _camera.ProjectionViewMatrix;

        var target = RenderTargetId.SceneLight;
        var passDesc = _renderPassDesc[(int)target];

        var commands = _commandSubmitter.DrainCommandQueue<DrawCommandLight>();
        _commandRenderer.RenderLightCommands(lightPass, commands);


    }

    public void Dispose()
    {
    }
}