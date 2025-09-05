
using ConcreteEngine.Core.Scene;

namespace ConcreteEngine.Core.Rendering;

internal interface IRender
{
    ICamera Camera { get; }
    RenderTargetEnumerator GetEnumerator();
    void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation);
    void RegisterRenderTargetsFrom(RenderTargetDescriptor desc);
    void PrepareRender(float alpha, in RenderGlobalSnapshot renderGlobals);
    void RenderScenePass(SceneRenderPass pass, RenderPipeline submitter);
    void RenderLightPass(LightRenderPass lightPass, RenderPipeline submitter);
}