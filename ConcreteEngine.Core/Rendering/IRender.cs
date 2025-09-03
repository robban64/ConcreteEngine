
namespace ConcreteEngine.Core.Rendering;

internal interface IRender
{
    ICamera Camera { get; }
    RenderTargetEnumerator GetEnumerator();
    void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation);
    void RegisterRenderTargetsFrom(RenderTargetDescriptor desc);
    void PrepareRender(float alpha);
    void RenderScenePass(SceneRenderPass pass, DrawCommandSubmitter submitter);
    void RenderLightPass(LightRenderPass lightPass, DrawCommandSubmitter submitter);
}