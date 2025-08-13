#region

using ConcreteEngine.Core.Rendering.SpriteBatching;
using ConcreteEngine.Core.Rendering.Tilemap;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderPipeline
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _ctx;
    
    private readonly DrawCommandCollector _commandCollector;
    private readonly DrawCommandSubmitter _commandSubmitter;
    private readonly DrawEmitterContext _emitterContext;

    private readonly SortedList<RenderTargetId, RenderPass> _renderPasses;
    private RenderPass? _currentRenderPass;

    private readonly SpriteBatcher _spriteBatch;
    private readonly TilemapBatcher _tilemapBatcher;
    
    public SpriteBatcher SpriteBatch =>  _spriteBatch;

    
    internal RenderPipeline(IGraphicsDevice graphics)
    {
        _graphics = graphics;
        _ctx = graphics.Ctx;
        _renderPasses = new SortedList<RenderTargetId, RenderPass>(_ctx.Configuration.MaxRenderPasses);

        _commandCollector = new DrawCommandCollector();
        _commandSubmitter = new DrawCommandSubmitter();

        _spriteBatch = new SpriteBatcher(graphics, this);
        _tilemapBatcher = new TilemapBatcher(graphics,64,32);

        _emitterContext = new DrawEmitterContext
        {
            Graphics = _graphics,
            SpriteBatch = _spriteBatch,
            TilemapBatch = _tilemapBatcher
        };

        CreateRenderPass(RenderTargetId.None, null);

    }

    public void RegisterCommand<T>(RenderTargetId target, int capacity = 32) where T : unmanaged, IDrawCommandMessage
        => _commandSubmitter.RegisterCommand<T>(target, capacity);
    
    public void RegisterEmitter<T>(int order, T emitter) where T : class, IDrawCommandEmitter
        => _commandCollector.RegisterEmitter<T>(order, emitter);

    public void CreateRenderPass(RenderTargetId renderPassId, IRenderTarget? renderTarget)
    {
        if (_renderPasses.ContainsKey(renderPassId))
            throw GraphicsException.ResourceAlreadyExists<RenderPass>(renderPassId);

        _renderPasses.Add(renderPassId, new RenderPass(renderPassId, renderTarget));
    }

    public void BindRenderPass(RenderTargetId renderPassId)
    {
        BindRenderPass(renderPassId, _ctx.ViewTransform.ProjectionViewMatrix);
    }

    public void BindRenderPass(RenderTargetId renderPassId, Matrix4X4<float> projViewMatrix)
    {
        _currentRenderPass = _renderPasses[renderPassId];
        _currentRenderPass.ProjectionViewMatrix = projViewMatrix;
    }

    internal void Prepare()
    {
    }

    internal void Execute()
    {
        _commandSubmitter.ClearData();
        
        _commandCollector.Collect(_emitterContext, _commandSubmitter);
        
        var tilemapCommands = _commandSubmitter.GetQueue<TilemapDrawCommand>(RenderTargetId.None);
        for (int i = 0; i < tilemapCommands.Length; i++)
        {
            ref var msg = ref tilemapCommands[i];
            _ctx.UseShader(msg.Cmd.ShaderId);
            _ctx.SetUniform(ShaderUniform.ProjectionViewMatrix, _ctx.ViewTransform.ProjectionViewMatrix);

            _ctx.SetUniform(ShaderUniform.ModelMatrix, in msg.Cmd.Transform);
            _ctx.SetUniform(ShaderUniform.SampleTexture, 0);

            _ctx.BindTexture(msg.Cmd.TextureId, 0);

            _ctx.BindMesh(msg.Cmd.MeshId);
            _ctx.DrawIndexed(msg.Cmd.DrawCount);
        }
        
        var spriteCommands = _commandSubmitter.GetQueue<SpriteDrawCommand>(RenderTargetId.None);
        for (int i = 0; i < spriteCommands.Length; i++)
        {
            ref var msg = ref spriteCommands[i];
            _ctx.UseShader(msg.Cmd.ShaderId);
            _ctx.SetUniform(ShaderUniform.ProjectionViewMatrix, _ctx.ViewTransform.ProjectionViewMatrix);

            _ctx.SetUniform(ShaderUniform.ModelMatrix, in msg.Cmd.Transform);
            _ctx.SetUniform(ShaderUniform.SampleTexture, 0);

            _ctx.BindTexture(msg.Cmd.TextureId, 0);

            _ctx.BindMesh(msg.Cmd.MeshId);
            _ctx.DrawIndexed(msg.Cmd.DrawCount);
        }


        /*
        foreach (var renderPass in _renderPasses.Values)
        {
            if (renderPass.HasData)
                renderPass.Execute(_ctx);
        }

        _currentRenderPass = null;
        */
    }
}