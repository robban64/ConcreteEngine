using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

internal interface IRender
{
    ICamera Camera { get; }
    bool TryGetNextPasses(out RenderTargetId targetId, out List<IRenderPassDescriptor> passes);
    void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation);
    void RegisterRenderTargetsFrom(in Vector2D<int> outputSize, RenderTargetDescriptor desc);
    void Prepare(float alpha, in RenderGlobalSnapshot renderGlobals);
    void RenderScenePass(IScenePass pass, RenderPipeline submitter);
    void RenderDepthPass(IDepthPass depthPass, RenderPipeline submitter);
}