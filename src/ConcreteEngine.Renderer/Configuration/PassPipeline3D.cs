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
                ctx.ActivateDepthMode(); // Note!

                ctx.GfxCmd.BeginRenderPass(ctx.Target.FboId, state.PassState);
                ctx.GfxCmd.ApplyStateFunctions(GfxDrawFunctions.MakeDepth());
                return PassAction.DrawPassResult();
            }).OnPassEnd(static (ctx, in _) =>
            {
                ctx.GfxCmd.EndRenderPass();
                ctx.RestoreMode();
            });

        // Scene 
        // Pass 1: draw scene 
        passPipeline.Register<ScenePassTag>(FboVariant.V0, new PassId(1), PassOp.Draw, RenderPassState.MakeSceneMsaa())
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.GfxCmd.BeginRenderPass(ctx.Target.FboId, state.PassState);
                ctx.GfxCmd.ApplyStateFunctions(GfxDrawFunctions.MakeDefault());
                return PassAction.DrawPassResult();
            });

        // Pass 2: draw scene effects
        passPipeline.RegisterContinue<ScenePassTag>(FboVariant.V0, new PassId(2), PassOp.Draw,
                RenderPassState.MakeSceneEffect())
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.ContinueFromRenderPass(ctx.Target.FboId, state.PassState.StateFlags);
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
                ctx.GfxCmd.BlitFramebuffer(state.TargetFboId, ctx.Target.FboId, state.LinearFilter);
                return PassAction.ResolveTargetResult();
            }).OnPassEnd(static (ctx, in _) =>
            {
                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<PostPassTag>(FboVariant.V0, TexSlot.Slot0(texId));

                ctx.GfxCmd.EndRenderPass();
                ctx.GenerateMips(texId);
            });

        // Post A
        passPipeline.Register<PostPassTag>(FboVariant.V0, new PassId(4), PassOp.Fsq,
                RenderPassState.MakePostProcess(shaders.CompositeShader))
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.GfxCmd.BeginRenderPass(ctx.Target.FboId, state.PassState);
                ctx.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.GfxCmd.EndRenderPass();

                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<PostPassTag>(FboVariant.V1, TexSlot.Slot0(texId));

                return PassAction.FsqPassResult();
            });

        // Post B
        passPipeline.Register<PostPassTag>(FboVariant.V1, new PassId(5), PassOp.Fsq,
                RenderPassState.MakePostProcess(shaders.ColorFilterShader))
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.GfxCmd.BeginRenderPass(ctx.Target.FboId, state.PassState);
                ctx.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.GfxCmd.EndRenderPass();

                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<OutputPassTag>(FboVariant.V0, TexSlot.Slot0(texId));

                return PassAction.FsqPassResult();
            });

        // Screen
        passPipeline.Register<OutputPassTag>(FboVariant.V0, new PassId(6), PassOp.Screen,
                RenderPassState.MakeScreen(shaders.PresentShader))
            .OnPassBegin(static (ctx, in state) =>
            {
                ctx.GfxCmd.BeginRenderPass(ctx.Target.FboId, state.PassState);
                ctx.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.GfxCmd.EndRenderPass();

                ctx.GfxCmd.ApplyPassState(GfxStateFlags.ColorMask);
                ctx.GfxCmd.Clear(ColorRgba.Black, ClearBufferFlag.ColorAndDepth);

                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SetOutputTexture(texId);

                return PassAction.ResolveTargetResult();
            });

        /*
        passPipeline.Register<ScreenPassTag>(FboVariant.Default, new PassId(6), PassOpKind.Screen,
                   RenderPassState.MakeScreen(defaults.PresentShader))
               .OnPassBegin(static (ctx, in state) =>
               {
                   var sources = ctx.GetPassSources();

                   ctx.BeginScreenPass(state.ClearColor, state.PassState);
                   ctx.DrawFullscreenQuad(state.ShaderId, sources);
                   return PassAction.ScreenPassResult();
               });
    */
    }
}