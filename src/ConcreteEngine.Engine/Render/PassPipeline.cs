using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics.Gfx;
using static ConcreteEngine.Graphics.Gfx.GfxStateFlags;

namespace ConcreteEngine.Engine.Render;

internal static partial class PassPipeline
{
    public static void RegisterPassPipeline(RenderPassPipeline passPipeline)
    {
        // Shadow
        passPipeline.Register<ShadowTarget>(FboVariant.V0, PassOp.Draw, RenderPassState.MakeShadow())
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.ActivateDepthMode(); // Note!

                ctx.Gfx.BeginRenderPass(ctx.FboId, state.PassState);
                ctx.Gfx.ApplyStateFunctions(GfxDrawFunctions.MakeDepth());
                return PassAction.DrawPassResult();
            }).OnPassEnd(static (ctx, _) =>
            {
                ctx.Gfx.EndRenderPass();
                ctx.RestoreMode();
            });

        // Scene 
        // Pass 1: draw scene 
        passPipeline.Register<SceneTarget>(FboVariant.V0, PassOp.Draw, RenderPassState.MakeSceneMsaa())
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BeginRenderPass(ctx.FboId, state.PassState);
                ctx.Gfx.ApplyStateFunctions(GfxDrawFunctions.MakeDefault());
                return PassAction.DrawPassResult();
            })
            .OnPassEnd(static (ctx, _) =>
            {
                ctx.MutateStatePass<SceneTarget>(FboVariant.V1, PassMutationState.MutateTarget(ctx.FboId));
                var selectionCount = RenderEcs.Store<SelectionComponent>().Count;
                var debugBoundsCount = RenderEcs.Store<DebugBoundsComponent>().Count;
                if (selectionCount + debugBoundsCount == 0) return;

                ctx.Gfx.BindFramebuffer(ctx.FboId);
                ctx.Gfx.ApplyPassState(Blend | Cull | Srgb | ColorMask | Ac2);

                if (selectionCount > 0) SelectionRenderer(ctx);
                if (debugBoundsCount > 0) DebugBoundsRenderer(ctx);
            });


        // Pass 3: resolve to scene FBO to post FBO
        passPipeline.Register<SceneTarget>(FboVariant.V1, PassOp.Resolve, RenderPassState.MakeResolve())
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BlitFramebuffer(state.TargetFboId, ctx.FboId, state.LinearFilter);
                return PassAction.ResolveTargetResult();
            }).OnPassEnd(static (ctx, _) =>
            {
                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<PostFxTarget>(FboVariant.V0, 0, texId);

                ctx.Gfx.EndRenderPass();
                ctx.Gfx.GenerateMipMaps(texId);
            });

        // Post A
        passPipeline.Register<PostFxTarget>(FboVariant.V0, PassOp.Fsq,
                RenderPassState.MakePostProcess(RenderRegistry.CompositeShader))
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BeginRenderPass(ctx.FboId, state.PassState);
                ctx.DrawCmd.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.Gfx.EndRenderPass();

                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<PostFxTarget>(FboVariant.V1, 0, texId);

                return PassAction.FsqPassResult();
            });

        // Post B
        passPipeline.Register<PostFxTarget>(FboVariant.V1, PassOp.Fsq,
                RenderPassState.MakePostProcess(RenderRegistry.ColorFilterShader))
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BeginRenderPass(ctx.FboId, state.PassState);
                ctx.DrawCmd.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.Gfx.EndRenderPass();

                var texId = ctx.Target.Attachments.ColorTexture;
                ctx.SampleTo<OutputTarget>(FboVariant.V0, 0, texId);

                return PassAction.FsqPassResult();
            });

        // Screen
        passPipeline.Register<OutputTarget>(FboVariant.V0, PassOp.Screen,
                RenderPassState.MakeScreen(RenderRegistry.PresentShader))
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BeginRenderPass(ctx.FboId, state.PassState);
                ctx.DrawCmd.DrawFullscreenQuad(state.ShaderId, ctx.GetPassSources());
                ctx.Gfx.EndRenderPass();

                ctx.Gfx.ApplyPassState(ColorMask);
                ctx.Gfx.Clear(ColorRgba.Black, ClearBufferFlag.ColorAndDepth);

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