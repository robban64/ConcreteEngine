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
    
    private readonly SortedList<RenderTargetId, RenderPass> _renderPasses;
    private RenderPass? _currentRenderPass;

    private readonly SpriteBatchController _spriteBatch;
    
    public SpriteBatchController SpriteBatch =>  _spriteBatch;
    
    internal RenderPipeline(IGraphicsDevice graphics)
    {
        _graphics = graphics;
        _ctx = graphics.Ctx;
        _renderPasses = new SortedList<RenderTargetId, RenderPass>(_ctx.Configuration.MaxRenderPasses);
        _spriteBatch = new SpriteBatchController(graphics, this);
        CreateRenderPass(RenderTargetId.None, null);
    }

    public void SubmitDraw<TCommand>(TCommand cmd) where TCommand : class, IDrawCommand
    {
        _currentRenderPass!.AddCommand(cmd);
    }

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
        foreach (var renderPass in _renderPasses.Values)
        {
            if (renderPass.HasData)
                renderPass.Execute(_ctx);
        }

        _currentRenderPass = null;
    }
}