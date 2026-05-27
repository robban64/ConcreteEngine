using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer.Configuration;

internal static class PassPipeline3D
{
    public static void RegisterPassPipeline(RenderPassPipeline passPipeline, in CoreShaders shaders)
    {
        // Shadow
        passPipeline.Register<ShadowPassTag>(FboVariant.V0, new PassId(0), PassOp.Draw, RenderPassState.MakeShadow())
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.Ops.ActivateDepthMode(); // Note!

                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.PassClear, state.PassState);
                ctx.Ops.ApplyStateFunctions(GfxPassFunctions.MakeDepth());
                return PassAction.DrawPassResult();
            }).OnPassEnd(static (ctx, in _) =>
            {
                ctx.Ops.EndRenderPass();
                ctx.Ops.RestoreMode();
            });

        // Scene 
        // Pass 1: draw scene 
        passPipeline.Register<ScenePassTag>(FboVariant.V0, new PassId(1), PassOp.Draw, RenderPassState.MakeSceneMsaa(4))
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.PassClear, state.PassState);
                ctx.Ops.ApplyStateFunctions(GfxPassFunctions.MakeDefault());
                return PassAction.DrawPassResult();
            });

        // Pass 2: draw scene effects
        passPipeline.RegisterContinue<ScenePassTag>(FboVariant.V0, new PassId(2), PassOp.Draw,
                RenderPassState.MakeSceneEffect(4))
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.Ops.ContinueFromRenderPass(ctx.Target.FboId, state.PassState);
                ctx.MutateStatePass<ScenePassTag>(
                    FboVariant.V1,
                    PassMutationState.MutateTarget(ctx.Target.FboId)
                );
                return PassAction.DrawEffectPassResult();
            });

        // Pass 3: resolve to scene FBO to post FBO
        passPipeline.Register<ScenePassTag>(FboVariant.V1, new PassId(3), PassOp.Resolve,
                RenderPassState.MakeResolve())
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.Ops.Blit(state.TargetFboId, ctx.Target.FboId, state.LinearFilter);
                return PassAction.ResolveTargetResult();
            }).OnPassEnd(static (ctx, in _) =>
            {
                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<PostPassTag>(FboVariant.V0, TexSlot.Slot0(texId));

                ctx.Ops.EndRenderPass();
                ctx.Ops.GenerateMips(texId);
            });

        // Post A
        passPipeline.Register<PostPassTag>(FboVariant.V0, new PassId(4), PassOp.Fsq,
                RenderPassState.MakePostProcess(shaders.CompositeShader))
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.PassClear, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.Ops.EndRenderPass();

                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<PostPassTag>(FboVariant.V1, TexSlot.Slot0(texId));

                return PassAction.FsqPassResult();
            });

        // Post B
        passPipeline.Register<PostPassTag>(FboVariant.V1, new PassId(5), PassOp.Fsq,
                RenderPassState.MakePostProcess(shaders.ColorFilterShader))
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.PassClear, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.Ops.EndRenderPass();

                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<OutputPassTag>(FboVariant.V0, TexSlot.Slot0(texId));

                return PassAction.FsqPassResult();
            });

        // Screen
        passPipeline.Register<OutputPassTag>(FboVariant.V0, new PassId(6), PassOp.Screen,
                RenderPassState.MakeScreen(shaders.PresentShader))
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.PassClear, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.Ops.EndRenderPass();

                ctx.Ops.ClearColor(new GfxPassClear(ColorRgba.Black, ClearBufferFlag.ColorAndDepth));
                ctx.Ops.ToggleStates(GfxPassState.Disable(GfxStateFlags.FramebufferSrgb));

                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.Ops.SetOutputTexture(texId);

                return PassAction.ResolveTargetResult();
            });

        /*
        passPipeline.Register<ScreenPassTag>(FboVariant.Default, new PassId(6), PassOpKind.Screen,
                   RenderPassState.MakeScreen(defaults.PresentShader))
               .OnPassBegin(static (ctx, in state) =>
               {
                   var sources = ctx.GetPassSources();

                   ctx.Ops.BeginScreenPass(state.ClearColor, state.PassState);
                   ctx.Ops.DrawFullscreenQuad(state.ShaderId, sources);
                   return PassAction.ScreenPassResult();
               });
    */
    }
}