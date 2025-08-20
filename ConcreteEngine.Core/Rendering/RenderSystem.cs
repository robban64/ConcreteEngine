#region

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Extensions;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Rendering.Batchers.Sprite;
using ConcreteEngine.Core.Rendering.Batchers.Tilemap;
using ConcreteEngine.Core.Rendering.Materials;
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

    private readonly Shader[] _shaders;
    private readonly MaterialStore _materialStore;
    private readonly List<DrawCommandId>[] _renderPasses;
    private readonly List<IRenderPass>[] _renderPassDesc;

    private readonly DrawEmitterCollector _emitterCollector;
    private readonly DrawCommandSubmitter _commandSubmitter;
    private readonly DrawEmitterContext _emitterContext;
    
    private readonly CommandRenderer  _commandRenderer;

    private readonly SpriteBatcher _spriteBatch;
    private readonly TilemapBatcher _tilemapBatcher;

    public SpriteBatcher SpriteBatch => _spriteBatch;

    internal RenderSystem(IGraphicsDevice graphics, ViewTransform2D camera, Shader[] shaders)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        _camera = camera;

        _shaders = shaders.ToArray();
        _materialStore = new MaterialStore();

        _renderPasses = new List<DrawCommandId>[RenderTargetCount];
        _renderPassDesc = new List<IRenderPass>[RenderTargetCount];
        for (int i = 0; i < RenderTargetCount; i++)
        {
            _renderPasses[i] = new List<DrawCommandId>(4);
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
            RegisterCommand( cmd.Target, cmd.CommandId, cmd.Capacity);
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

    public void RegisterCommand(RenderTargetId target, DrawCommandId commandId, int capacity)
    {
        _commandSubmitter.RegisterCommand(commandId, target, capacity);
        _renderPasses[(int)target].Add(commandId);
    }

    public void AddMaterial(MaterialDescription description)
        => _materialStore.AddMaterial(description);


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
        var pass = _renderPasses[(int)target];

        foreach (var commandId in pass)
        {
            var commands = _commandSubmitter.SceneQueue.GetCmdQueue(commandId);
            //var meta = _commandSubmitter.SceneQueue.GetMetaQueue(commandId);
            _commandRenderer.DrawMeshCommands(commands);
        }
    }

    private void RenderLightPass(LightRenderPass lightPass)
    {
        var projView = _camera.ProjectionViewMatrix;

        var target = RenderTargetId.SceneLight;
        var passDesc = _renderPassDesc[(int)target];
        var passCommands = _renderPasses[(int)target];

        for (int p = 0; p < passCommands.Count; p++)
        {
            var commandId = passCommands[p];
            //var pass = passDesc[p];
            var commands = _commandSubmitter.LightQueue.GetCmdQueue(commandId);
            //var meta = _commandSubmitter.LightQueue.GetMetaQueue(commandId);

            _commandRenderer.RenderLightCommands(lightPass, commands);

        }
    }

    public void Dispose()
    {
    }
}