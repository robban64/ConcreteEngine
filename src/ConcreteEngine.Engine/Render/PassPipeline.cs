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
        passPipeline.Register<ShadowTarget>(FboVariant.V0, PassOp.Draw, MakeShadow())
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.ActivateDepthMode(); // Note!

                ctx.Gfx.BeginRenderPass(ctx.TargetFbo, state);
                ctx.Gfx.ApplyStateFunctions(GfxDrawFunctions.MakeDepth());
                return PassAction.DrawPassResult();
            }).OnPassEnd(static (ctx, _) =>
            {
                ctx.Gfx.EndRenderPass();
                ctx.RestoreMode();
            });

        // Scene 
        // Pass 1: draw scene 
        passPipeline.Register<SceneTarget>(FboVariant.V0, PassOp.Draw, MakeSceneMsaa())
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BeginRenderPass(ctx.TargetFbo, state);
                ctx.Gfx.ApplyStateFunctions(GfxDrawFunctions.MakeDefault());
                return PassAction.DrawPassResult();
            })
            .OnPassEnd(static (ctx, _) =>
            {
                ctx.MutateStatePass<SceneTarget>(FboVariant.V1, ctx.TargetFbo);
                
                var selectionCount = RenderEcs.Store<SelectionComponent>().Count;
                var debugBoundsCount = RenderEcs.Store<DebugBoundsComponent>().Count;
                if (selectionCount + debugBoundsCount == 0) return;

                ctx.Gfx.BindFramebuffer(ctx.TargetFbo);
                ctx.Gfx.ApplyPassState(Blend | Cull | Srgb | ColorMask | Ac2);

                if (selectionCount > 0) SelectionRenderer(ctx);
                if (debugBoundsCount > 0) DebugBoundsRenderer(ctx);
            });


        // Pass 3: resolve to scene FBO to post FBO
        passPipeline.Register<SceneTarget>(FboVariant.V1, PassOp.Resolve, MakeResolve())
            .OnPassBegin(static (ctx, _) =>
            {
                var passState = ctx.State;
                ctx.Gfx.BlitFramebuffer(passState.ResolveTarget, passState.Target, passState.LinearFilter);

                var texId = ctx.TargetMeta.Attachments.ColorTexture;
                ctx.SampleTo<PostFxTarget>(FboVariant.V0, 0, texId);

                ctx.Gfx.EndRenderPass();
                ctx.Gfx.GenerateMipMaps(texId);

                return PassAction.ResolveTargetResult();
            });

        // Post A
        passPipeline.Register<PostFxTarget>(FboVariant.V0, PassOp.Fsq, MakePostFx(), RenderRegistry.CompositeShader)
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BeginRenderPass(ctx.TargetFbo, state);
                ctx.DrawCmd.DrawFullscreenQuad(ctx.PassShader, ctx.GetPassSources());
                ctx.Gfx.EndRenderPass();

                var texId = ctx.TargetMeta.Attachments.ColorTexture;
                ctx.SampleTo<PostFxTarget>(FboVariant.V1, 0, texId);

                return PassAction.FsqPassResult();
            });

        // Post B
        passPipeline.Register<PostFxTarget>(FboVariant.V1, PassOp.Fsq, MakePostFx(), RenderRegistry.ColorFilterShader)
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BeginRenderPass(ctx.TargetFbo, state);
                ctx.DrawCmd.DrawFullscreenQuad(ctx.PassShader, ctx.GetPassSources());
                ctx.Gfx.EndRenderPass();

                var texId = ctx.TargetMeta.Attachments.ColorTexture;
                ctx.SampleTo<OutputTarget>(FboVariant.V0, 0, texId);

                return PassAction.FsqPassResult();
            });

        // Screen
        passPipeline.Register<OutputTarget>(FboVariant.V0, PassOp.Screen, MakeScreen(), RenderRegistry.PresentShader)
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BeginRenderPass(ctx.TargetFbo, state);
                ctx.DrawCmd.DrawFullscreenQuad(ctx.PassShader, ctx.GetPassSources());
                ctx.Gfx.EndRenderPass();

                ctx.Gfx.ApplyPassState(ColorMask);
                ctx.Gfx.Clear(ColorRgba.Black, ClearBufferFlag.ColorAndDepth);

                var texId = ctx.TargetMeta.Attachments.ColorTexture;
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
    
    private static GfxPassState MakeSceneMsaa() =>
        GfxPassState.MakeColorDepthClear(Color4.Black,DepthTest | DepthWrite | Cull | Srgb | ColorMask | Ac2);

    private static GfxPassState MakeResolve() => GfxPassState.MakeNoClear(ColorMask);

    private static GfxPassState MakePostFx() => GfxPassState.MakeColorClear(Color4.Black, ColorMask | Srgb);

    private static GfxPassState MakeScreen() => GfxPassState.MakeColorClear(Color4.Black, ColorMask | Srgb);

    private static GfxPassState MakeShadow() => GfxPassState.MakeDepthClear(DepthTest | DepthWrite | Cull | Srgb | PolygonOffset | Ac2);

    private static GfxPassState MakeSceneEffect() => GfxPassState.MakeNoClear(Blend | Cull | Srgb | ColorMask | Ac2);

}