using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Rendering;

internal static class TempPassSetup
{
    public static void RegisterPassPipeline(RenderPassPipeline passPipeline, ShaderId compositeShader,
        ShaderId presentShader, ShaderId colorFilterShader)
    {
        // Shadow
        passPipeline.Register<ShadowPassTag>(FboVariant.Default, new PassId(0), PassOpKind.Draw,
                RenderPassState.MakeShadow())
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.Ops.ActivateDepthMode(); // Note!

                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.Ops.ApplyStateFunctions(GfxPassStateFunc.MakeDepth());
                return PassAction.DrawPassResult();
            }).OnPassEnd(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.Ops.EndRenderPass();
                ctx.Ops.RestoreMode();
            });

        // Scene 
        // Pass 0: draw scene into MSAA FBO
        passPipeline.Register<ScenePassTag>(FboVariant.Default, new PassId(1), PassOpKind.Draw,
                RenderPassState.MakeSceneMsaa(4))
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.Ops.ApplyStateFunctions(GfxPassStateFunc.MakeDefault());

                ctx.MutateStatePass<ScenePassTag>(
                    FboVariant.Secondary,
                    PassMutationState.MutateTarget(ctx.Target.FboId)
                );
                return PassAction.DrawPassResult();
            });

        // Pass 1: resolve to scene FBO
        passPipeline.Register<ScenePassTag>(FboVariant.Secondary, new PassId(2), PassOpKind.Resolve,
                RenderPassState.MakeResolve())
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.Ops.ToggleStates(new GfxPassState { FramebufferSrgb = false });
                ctx.Ops.Blit(state.TargetFboId, ctx.Target.FboId, state.LinearFilter);
                return PassAction.ResolveTargetResult();
            }).OnPassEnd(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<PostPassTag>(FboVariant.Default, TextureSlot.Slot0(texId));

                ctx.Ops.EndRenderPass();
                ctx.Ops.GenerateMips(texId);
            });

        // Post A
        passPipeline.Register<PostPassTag>(FboVariant.Default, new PassId(3), PassOpKind.Fsq,
                RenderPassState.MakePostProcess(compositeShader))
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var sources = ctx.GetPassSources();
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, sources);
                return PassAction.FsqPassResult();
            }).OnPassEnd(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<PostPassTag>(FboVariant.Secondary, TextureSlot.Slot0(texId));

                ctx.Ops.EndRenderPass();
            });

        // Post B
        passPipeline.Register<PostPassTag>(FboVariant.Secondary, new PassId(4), PassOpKind.Fsq,
                RenderPassState.MakePostProcess(colorFilterShader))
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var sources = ctx.GetPassSources();
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, sources);
                return PassAction.FsqPassResult();
            }).OnPassEnd(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<ScreenPassTag>(FboVariant.Default, TextureSlot.Slot0(texId));

                ctx.Ops.EndRenderPass();
            });

        // Screen
        passPipeline.Register<ScreenPassTag>(FboVariant.Default, new PassId(5), PassOpKind.Screen,
                RenderPassState.MakeScreen(presentShader))
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var sources = ctx.GetPassSources();

                ctx.Ops.BeginScreenPass(state.ClearColor, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, sources);
                return PassAction.ScreenPassResult();
            });
    }
}