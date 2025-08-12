#region

using ConcreteEngine.Core.Rendering.Sprite;
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

    private readonly SpriteBatchController _spriteBatch;
    
    public SpriteBatchController SpriteBatch =>  _spriteBatch;

    
    internal RenderPipeline(IGraphicsDevice graphics)
    {
        _graphics = graphics;
        _ctx = graphics.Ctx;
        _renderPasses = new SortedList<RenderTargetId, RenderPass>(_ctx.Configuration.MaxRenderPasses);

        _commandCollector = new DrawCommandCollector();
        _commandSubmitter = new DrawCommandSubmitter();

        _spriteBatch = new SpriteBatchController(graphics, this);

        _emitterContext = new DrawEmitterContext
        {
            Graphics = _graphics,
            SpriteBatch = _spriteBatch,
        };

        CreateRenderPass(RenderTargetId.None, null);

    }

    public void RegisterCommand<T>(RenderTargetId target, int capacity = 32) where T : unmanaged, IDrawCommandMessage
        => _commandSubmitter.RegisterCommand<T>(target, capacity);
    
    public void RegisterEmitter<T>(T emitter) where T : class, IDrawCommandEmitter
        => _commandCollector.RegisterEmitter<T>(emitter);

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
        _commandSubmitter.Clear();
        
        _commandCollector.Collect(_emitterContext, _commandSubmitter);
        var commands = _commandSubmitter.GetQueue<SpriteDrawCommand>(RenderTargetId.None);
        foreach (var (cmd, meta) in commands)
        {
            _ctx.UseShader(cmd.ShaderId);
            _ctx.SetUniform(ShaderUniform.ProjectionViewMatrix, _ctx.ViewTransform.ProjectionViewMatrix);

            _ctx.SetUniform(ShaderUniform.ModelMatrix, in cmd.Transform);
            _ctx.SetUniform(ShaderUniform.SampleTexture, 0);

            _ctx.BindTexture(cmd.TextureId, 0);

            _ctx.BindMesh(cmd.MeshId);
            _ctx.DrawIndexed(cmd.DrawCount);

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