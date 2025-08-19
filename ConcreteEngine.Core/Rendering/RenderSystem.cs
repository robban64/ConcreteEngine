#region

using System.Numerics;
using System.Runtime.CompilerServices;
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
    private readonly SortedList<int, DrawCommandId>[] _renderPasses;
    private readonly SortedList<int, RenderPassData>[] _renderPassDesc;

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

        _renderPasses = new SortedList<int, DrawCommandId>[RenderTargetCount];
        _renderPassDesc = new SortedList<int, RenderPassData>[RenderTargetCount];
        for (int i = 0; i < RenderTargetCount; i++)
        {
            _renderPasses[i] = new SortedList<int, DrawCommandId>(4);
            _renderPassDesc[i] = new SortedList<int, RenderPassData>(4);
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
        
        foreach (var (order, meta) in builder.Passes)
            RegisterRenderPass(meta.Target, order, meta.Param);
        
        foreach (var (order, meta) in builder.Commands)
            RegisterCommand(order, meta.CommandId, meta.Target, meta.Capacity);
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
    
    public void RegisterRenderPass(RenderTargetId target, int order, in RenderPassData param)
    {
        if (param.Op == RenderPassOp.FullscreenQuad && param.SourceTexId == null)
        {
            throw new InvalidOperationException(
                $"FullscreenQuad requires {nameof(param.SourceTexId)} (source texture).");
        }

        if (param.Op == RenderPassOp.Blit && param.SourceTexId != null)
        {
            throw new InvalidOperationException(
                $"Blit op requires {nameof(param.BlitFboId)} (source framebuffer).");
        }

        _renderPassDesc[(int)target].Add(order, param);
    }

    public void RegisterCommand(int order, DrawCommandId commandId, RenderTargetId target, int capacity)
    {
        _commandSubmitter.RegisterCommand(commandId, target, capacity);
        _renderPasses[(int)target].Add(order, commandId);
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
        _commandSubmitter.ResetBufferPointer();
        _emitterCollector.Collect(_emitterContext, _commandSubmitter);
    }

    private void Execute(float alpha)
    {
        for (int target = 0; target < 1; target++)
        {
            var renderTarget = (RenderTargetId)target;
            var passList = _renderPassDesc[target];
            for (int p = 0; p < passList.Count; p++)
            {
                var pass = passList[p];
                var (prevBlend, prevDepthTest) = (_gfx.BlendMode, _gfx.DepthTest);
                _gfx.SetBlendMode(pass.Blend);
                _gfx.SetDepthTest(pass.DepthTest);

                ExecutePass(renderTarget, in pass);

                _gfx.SetBlendMode(prevBlend);
                _gfx.SetDepthTest(prevDepthTest);
            }
        }
    }

    private void ExecutePass(RenderTargetId target, in RenderPassData pass)
    {
        if (pass.Op == RenderPassOp.Blit)
        {
            // preserves bindings internally
            _gfx.BlitFramebuffer(pass.BlitFboId!.Value, pass.TargetFboId, linearFilter: true);
            return;
        }

        var isScreenPass = pass.TargetFboId == default;

        if (pass.TargetFboId == default)
            _gfx.BeginScreenPass(pass.DoClear ? pass.ClearColor : null, pass.ClearMask);
        else
            _gfx.BeginRenderPass(pass.TargetFboId, pass.DoClear ? pass.ClearColor : null, pass.ClearMask);


        if (pass.Op == RenderPassOp.DrawScene)
        {
            ExecuteDrawScenePass(target);
            _gfx.EndRenderPass();
        }
        else if (pass.Op == RenderPassOp.FullscreenQuad)
        {
            var colTex = pass.SourceTexId!.Value;
            DrawFboScreenQuad(colTex, pass.ShaderId);
        }

        if (!isScreenPass)
        {
            _gfx.EndRenderPass();
        }
    }

    private void ExecuteDrawScenePass(RenderTargetId target)
    {
        var projView = _camera.ProjectionViewMatrix;
        foreach (var (_, commandId) in _renderPasses[(int)target])
        {
            var commands = _commandSubmitter.GetQueue(target, commandId);

            for (int i = 0; i < commands.Length; i++)
            {
                ref readonly var msg = ref commands[i];
                Draw(in msg.Cmd, in msg.Info, in projView);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Draw(in DrawCommandData data, in DrawCommandMeta meta, in Matrix4x4 projView)
    {
        var material = _materialStore[data.MaterialId];
        material.Bind(_gfx);
        _gfx.UseShader(material.Shader.ResourceId);
        _gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, in projView);

        _gfx.SetUniform(ShaderUniform.ModelMatrix, in data.Transform);
        _gfx.BindMesh(data.MeshId);
        _gfx.DrawIndexed(data.DrawCount);
    }

    private void DrawFboScreenQuad(TextureId colTexId, ShaderId shaderId)
    {
        _gfx.UseShader(shaderId);
        _gfx.BindTexture(colTexId, 0);
        _gfx.BindMesh(_graphics.QuadMeshId);
        _gfx.Draw();
    }

    public void Dispose()
    {
    }
}