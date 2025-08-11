#region

using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Rendering;

public sealed class RenderPipeline
{
    private readonly IGraphicsContext _ctx;
    private readonly SortedList<RenderTargetId, RenderPass> _renderPasses;

    private RenderPass? _currentRenderPass;

    internal RenderPipeline(IGraphicsContext ctx)
    {
        _ctx = ctx;
        _renderPasses = new SortedList<RenderTargetId, RenderPass>(_ctx.Configuration.MaxRenderPasses);
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