using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics.Gfx;
using static ConcreteEngine.Graphics.Gfx.GfxStateFlags;

namespace ConcreteEngine.Engine.Render;

internal static partial class PassPipeline
{
        
    private static void ActivateDepthMode()
    {
        RenderContext.ApplyForDepthPass();
        VisualSystem.Instance.UploadShadow();
        VisualSystem.Instance.UploadLightView();
    }

    private static void RestoreMode()
    {
        RenderContext.ResetContext();
        VisualSystem.Instance.UploadMainView();
    }

    public static void RegisterPassPipeline()
    {
        var registry = RenderRegistry.Instance;

        // Shadow
        registry.RegisterPass<ShadowTarget>(FboVariant.V0, PassOp.Draw, MakeShadow())
            .OnPassBegin(static (ctx, state) =>
            {
                ActivateDepthMode(); // Note!

                ctx.Gfx.BeginRenderPass(ctx.TargetFbo, state);
                ctx.Gfx.ApplyStateFunctions(GfxDrawFunctions.MakeDepth());
                return PassAction.DrawPassResult();
            }).OnPassEnd(static (ctx, _) =>
            {
                ctx.Gfx.EndRenderPass();
                RestoreMode();
            });

        // Scene 
        // Pass 1: draw scene 
        registry.RegisterPass<SceneTarget>(FboVariant.V0, PassOp.Draw, MakeSceneMsaa())
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
        registry.RegisterPass<SceneTarget>(FboVariant.V1, PassOp.Resolve, MakeResolve())
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
        registry.RegisterPass<PostFxTarget>(FboVariant.V0, PassOp.Fsq, MakePostFx(), RenderStore.CompositeShader)
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BeginRenderPass(ctx.TargetFbo, state);
                ctx.DrawFullscreenQuad();
                ctx.Gfx.EndRenderPass();

                var texId = ctx.TargetMeta.Attachments.ColorTexture;
                ctx.SampleTo<PostFxTarget>(FboVariant.V1, 0, texId);

                return PassAction.FsqPassResult();
            });

        // Post B
        registry.RegisterPass<PostFxTarget>(FboVariant.V1, PassOp.Fsq, MakePostFx(), RenderStore.ColorFilterShader)
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BeginRenderPass(ctx.TargetFbo, state);
                ctx.DrawFullscreenQuad();
                ctx.Gfx.EndRenderPass();

                var texId = ctx.TargetMeta.Attachments.ColorTexture;
                ctx.SampleTo<OutputTarget>(FboVariant.V0, 0, texId);

                return PassAction.FsqPassResult();
            });

        // Screen
        registry.RegisterPass<OutputTarget>(FboVariant.V0, PassOp.Screen, MakeScreen(), RenderStore.PresentShader)
            .OnPassBegin(static (ctx, state) =>
            {
                ctx.Gfx.BeginRenderPass(ctx.TargetFbo, state);
                ctx.DrawFullscreenQuad();
                ctx.Gfx.EndRenderPass();

                ctx.Gfx.ApplyPassState(ColorMask);
                ctx.Gfx.Clear(ColorRgba.Black, ClearBufferFlag.ColorAndDepth);

                var texId = ctx.TargetMeta.Attachments.ColorTexture;
                RenderContext.OutputTexture = texId;

                return PassAction.ResolveTargetResult();
            });
        
    }

    public static void RegisterFrameBuffers()
    {
        var registry = RenderRegistry.Instance;
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