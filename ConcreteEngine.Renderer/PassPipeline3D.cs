using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer;

internal static class PassPipeline3D
{
    public static void RegisterPassPipeline(RenderPassPipeline passPipeline, in RenderCoreShaders defaults)
    {
        // Shadow
        passPipeline.Register<ShadowPassTag>(FboVariant.Default, new PassId(0), PassOpKind.Draw,
                RenderPassState.MakeShadow())
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.Ops.ActivateDepthMode(); // Note!

                ctx.Ops.BeginRenderPass(ctx.Target.FboId, in state.ClearColor, state.PassState);
                ctx.Ops.ApplyStateFunctions(GfxPassStateFunc.MakeDepth());
                return PassAction.DrawPassResult();
            }).OnPassEnd(static (ctx, in state) =>
            {
                ctx.Ops.EndRenderPass();
                ctx.Ops.RestoreMode();
            });

        // Scene 
        // Pass 1: draw scene 
        passPipeline.Register<ScenePassTag>(FboVariant.Default, new PassId(1), PassOpKind.Draw,
                RenderPassState.MakeSceneMsaa(4))
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, in state.ClearColor, state.PassState);
                ctx.Ops.ApplyStateFunctions(GfxPassStateFunc.MakeDefault());
                return PassAction.DrawPassResult();
            });

        // Pass 2: draw scene effects
        passPipeline.RegisterContinue<ScenePassTag>(FboVariant.Default, new PassId(2), PassOpKind.Draw,
                RenderPassState.MakeSceneEffect(4))
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.Ops.ContinueFromRenderPass(ctx.Target.FboId, state.PassState);

                ctx.MutateStatePass<ScenePassTag>(
                    FboVariant.Secondary,
                    PassMutationState.MutateTarget(ctx.Target.FboId)
                );
                return PassAction.DrawEffectPassResult();
            });

        // Pass 3: resolve to scene FBO
        passPipeline.Register<ScenePassTag>(FboVariant.Secondary, new PassId(3), PassOpKind.Resolve,
                RenderPassState.MakeResolve())
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.Ops.Blit(state.TargetFboId, ctx.Target.FboId, state.LinearFilter);
                return PassAction.ResolveTargetResult();
            }).OnPassEnd(static (ctx, in _) =>
            {
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<PostPassTag>(FboVariant.Default, TextureSlot.Slot0(texId));

                ctx.Ops.EndRenderPass();
                ctx.Ops.GenerateMips(texId);
            });

        // Post A
        passPipeline.Register<PostPassTag>(FboVariant.Default, new PassId(4), PassOpKind.Fsq,
                RenderPassState.MakePostProcess(defaults.CompositeShader))
            .OnPassBegin(static (ctx, in state) =>
            {
                var sources = ctx.GetPassSources();
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, in state.ClearColor, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, sources);
                return PassAction.FsqPassResult();
            }).OnPassEnd(static (ctx, in _) =>
            {
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<PostPassTag>(FboVariant.Secondary, TextureSlot.Slot0(texId));

                ctx.Ops.EndRenderPass();
            });

        // Post B
        passPipeline.Register<PostPassTag>(FboVariant.Secondary, new PassId(5), PassOpKind.Fsq,
                RenderPassState.MakePostProcess(defaults.ColorFilterShader))
            .OnPassBegin(static (ctx, in state) =>
            {
                var sources = ctx.GetPassSources();
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, in state.ClearColor, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, sources);
                return PassAction.FsqPassResult();
            }).OnPassEnd(static (ctx, in _) =>
            {
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<ScreenPassTag>(FboVariant.Default, TextureSlot.Slot0(texId));

                ctx.Ops.EndRenderPass();
            });

        // Screen
        passPipeline.Register<ScreenPassTag>(FboVariant.Default, new PassId(6), PassOpKind.Screen,
                RenderPassState.MakeScreen(defaults.PresentShader))
            .OnPassBegin(static (ctx, in state) =>
            {
                var sources = ctx.GetPassSources();

                ctx.Ops.BeginScreenPass(in state.ClearColor, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, sources);
                return PassAction.ScreenPassResult();
            });
    }
}