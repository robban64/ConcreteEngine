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
            DrawRenderPassQuad(fsqPass);
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
            var meta = _commandSubmitter.SceneQueue.GetMetaQueue(commandId);

            for (int i = 0; i < commands.Length; i++)
            {
                ref readonly var cmd = ref commands[i];
                ref readonly var m = ref meta[i];

                Draw(in cmd, in m, in projView);
            }
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
            var pass = passDesc[p];
            var commands = _commandSubmitter.LightQueue.GetCmdQueue(commandId);
            var meta = _commandSubmitter.LightQueue.GetMetaQueue(commandId);

            for (int c = 0; c < commands.Length; c++)
            {
                ref readonly var cmd = ref commands[c];
                ref readonly var m = ref meta[c];

                DrawLightQuad(lightPass, in cmd, in m, in projView);
            }
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Draw(in DrawCommandMesh cmd, in DrawCommandMeta meta, in Matrix4x4 projView)
    {
        var material = _materialStore[cmd.MaterialId];
        material.Bind(_gfx);
        _gfx.UseShader(material.Shader.ResourceId);
        _gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, in projView);

        _gfx.SetUniform(ShaderUniform.ModelMatrix, in cmd.Transform);
        _gfx.BindMesh(cmd.MeshId);
        _gfx.DrawIndexed(cmd.DrawCount);
    }

    private void DrawRenderPassQuad(FsqRenderPass pass)
    {
        ArgumentNullException.ThrowIfNull(pass);
        ArgumentNullException.ThrowIfNull(pass.SourceTextures);
        ArgumentOutOfRangeException.ThrowIfZero(pass.SourceTextures.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pass.SourceTextures.Length, 4, nameof(pass.SourceTextures));

        var viewport = _camera.ViewportSize;
        _gfx.UseShader(pass.Shader);
        _gfx.SetUniform(ShaderUniform.TexelSize, viewport.ToSystemVec2() * pass.SizeRatio);

        for (int i = 0; i < pass.SourceTextures.Length; i++)
        {
            _gfx.BindTexture(pass.SourceTextures[i], (uint)i);
        }

        _gfx.BindMesh(_graphics.QuadMeshId);
        _gfx.Draw();
    }

    private void DrawLightQuad(LightRenderPass pass, in DrawCommandLight cmd, in DrawCommandMeta meta,
        in Matrix4x4 projView)
    {
        _gfx.UseShader(pass.Shader);
        _gfx.BindMesh(_graphics.QuadMeshId);

        _gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, in projView);


        _gfx.SetUniform(ShaderUniform.LightPos, cmd.Position);
        _gfx.SetUniform(ShaderUniform.Radius, cmd.Radius);
        _gfx.SetUniform(ShaderUniform.Color, cmd.Color);
        _gfx.SetUniform(ShaderUniform.Intensity, cmd.Intensity);
        _gfx.SetUniform(ShaderUniform.Softness, 2.5f);
        _gfx.SetUniform(ShaderUniform.Shape, 0);

        _gfx.Draw();
    }

/*

*/
    public void Dispose()
    {
    }
}