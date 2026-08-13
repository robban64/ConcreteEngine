using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

internal static partial class PassPipeline
{

    public static void RegisterPassPipeline(RenderPassPipeline passPipeline)
    {
        // Shadow
        passPipeline.Register<ShadowTarget>(FboVariant.V0, new PassId(0), PassOp.Draw, RenderPassState.MakeShadow())
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.ActivateDepthMode(); // Note!

                ctx.Cmd.BeginRenderPass(ctx.FboId, state.PassState);
                ctx.Cmd.ApplyStateFunctions(GfxDrawFunctions.MakeDepth());
                return PassAction.DrawPassResult();
            }).OnPassEnd(static (ctx, _) =>
            {
                ctx.Cmd.EndRenderPass();
                ctx.RestoreMode();
            });

        // Scene 
        // Pass 1: draw scene 
        passPipeline.Register<SceneTarget>(FboVariant.V0, new PassId(1), PassOp.Draw, RenderPassState.MakeSceneMsaa())
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Cmd.BeginRenderPass(ctx.FboId, state.PassState);
                ctx.Cmd.ApplyStateFunctions(GfxDrawFunctions.MakeDefault());
                return PassAction.DrawPassResult();
            });

        // Pass 2: draw scene effects
        passPipeline.RegisterContinue<SceneTarget>(FboVariant.V0, new PassId(2), PassOp.Continue,
            RenderPassState.MakeSceneEffect()).OnPassBegin(static (ctx, state) =>
        {
            ctx.Cmd.BindFramebuffer(ctx.FboId);
            ctx.Cmd.ApplyPassState(state.PassState.StateFlags);

            ctx.MutateStatePass<SceneTarget>(FboVariant.V1, PassMutationState.MutateTarget(ctx.FboId));

            if (RenderEcs.Store<SelectionComponent>().Count > 0)
                SelectionRenderer(ctx);

            if (RenderEcs.Store<DebugBoundsComponent>().Count > 0)
                DebugBoundsRenderer(ctx);
            return new PassAction(PassOp.Continue);
        });

        // Pass 3: resolve to scene FBO to post FBO
        passPipeline.Register<SceneTarget>(FboVariant.V1, new PassId(3), PassOp.Resolve,
                RenderPassState.MakeResolve())
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Cmd.BlitFramebuffer(state.TargetFboId, ctx.FboId, state.LinearFilter);
                return PassAction.ResolveTargetResult();
            }).OnPassEnd(static (ctx, _) =>
            {
                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<PostFxTarget>(FboVariant.V0, 0, texId);

                ctx.Cmd.EndRenderPass();
                ctx.Cmd.GenerateMipMaps(texId);
            });

        // Post A
        passPipeline.Register<PostFxTarget>(FboVariant.V0, new PassId(4), PassOp.Fsq,
                RenderPassState.MakePostProcess(RenderRegistry.CompositeShader))
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Cmd.BeginRenderPass(ctx.FboId, state.PassState);
                ctx.DrawCmdProcessor.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.Cmd.EndRenderPass();

                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<PostFxTarget>(FboVariant.V1, 0, texId);

                return PassAction.FsqPassResult();
            });

        // Post B
        passPipeline.Register<PostFxTarget>(FboVariant.V1, new PassId(5), PassOp.Fsq,
                RenderPassState.MakePostProcess(RenderRegistry.ColorFilterShader))
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Cmd.BeginRenderPass(ctx.FboId, state.PassState);
                ctx.DrawCmdProcessor.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.Cmd.EndRenderPass();

                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<OutputTarget>(FboVariant.V0, 0, texId);

                return PassAction.FsqPassResult();
            });

        // Screen
        passPipeline.Register<OutputTarget>(FboVariant.V0, new PassId(6), PassOp.Screen,
                RenderPassState.MakeScreen(RenderRegistry.PresentShader))
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Cmd.BeginRenderPass(ctx.FboId, state.PassState);
                ctx.DrawCmdProcessor.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.Cmd.EndRenderPass();

                ctx.Cmd.ApplyPassState(GfxStateFlags.ColorMask);
                ctx.Cmd.Clear(ColorRgba.Black, ClearBufferFlag.ColorAndDepth);

                var texId = ctx.Target.Attachments.ColorTexture;
                RenderContext.OutputTexture = texId;

                return PassAction.ResolveTargetResult();
            });
    }

    public static void RegisterFrameBuffers(RenderRegistry registry)
    {
        var outputSize = EngineWindow.Viewport.Size;
        var shadowSize = new Size2D(VisualManager.Instance.Shadow.ShadowMapSize);
        registry.Register<ShadowTarget>(FboVariant.V0, new CreateFboInfo(shadowSize)
            .AttachDepthTexture(FboDepthAttachment.Default()), FboResizeMode.Fixed);

        registry.Register<SceneTarget>(FboVariant.V0, new CreateFboInfo(outputSize)
            .AttachColorTexture(FboColorAttachment.Off(), RenderBufferMsaa.X4).AttachDepthStencilBuffer());

        registry.Register<SceneTarget>(FboVariant.V1, new CreateFboInfo(outputSize)
            .AttachColorTexture(FboColorAttachment.DefaultMip()).AttachDepthStencilBuffer());

        registry.Register<PostFxTarget>(FboVariant.V0, new CreateFboInfo(outputSize)
            .AttachColorTexture(FboColorAttachment.Default()));

        registry.Register<PostFxTarget>(FboVariant.V1, new CreateFboInfo(outputSize)
            .AttachColorTexture(FboColorAttachment.Default()));

        registry.Register<OutputTarget>(FboVariant.V0, new CreateFboInfo(outputSize)
            .AttachColorTexture(FboColorAttachment.Default()));
    }

}