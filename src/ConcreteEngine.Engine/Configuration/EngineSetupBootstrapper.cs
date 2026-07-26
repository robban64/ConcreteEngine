using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Gateway;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Configuration;
using ConcreteEngine.Renderer.Core;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Engine.Configuration;

internal sealed class EngineSetupCtx
{
    public required GraphicsRuntime Graphics;
    public required EngineGateway EngineGateway;
    public required EngineTickHub TickHub;

    public required CommandBus CommandBus;
    public required AssetSystem Assets;
    public required EngineRenderSystem Renderer;
    public required SceneSystem SceneSystem;
}

internal static class EngineSetupBootstrapper
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RegisterSteps(EngineSetupPipeline pipeline, EngineSetupCtx ctx)
    {
        pipeline.RegisterStep(EngineSetupState.NotStarted, ctx, OnNotStarted);
        pipeline.RegisterStep(EngineSetupState.LoadAssets, ctx, OnLoadAssets);
        pipeline.RegisterStep(EngineSetupState.SetupRenderer, ctx, OnSetupRender);
        pipeline.RegisterStep(EngineSetupState.SetupInternal, ctx, OnSetupInternal);
        pipeline.RegisterStep(EngineSetupState.LoadWorld, ctx, OnLoadWorld);
        pipeline.RegisterStep(EngineSetupState.LoadScene, ctx, OnLoadScene);
        pipeline.RegisterStep(EngineSetupState.LoadEditor, ctx, OnLoadEditor);
        //pipeline.RegisterRunner(EngineSetupState.Warmup, 60, ctx, OnWarmup);
        pipeline.RegisterStep(EngineSetupState.Final, ctx, OnDone);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnNotStarted(EngineSetupCtx ctx)
    {
        EngineWarmup.LoadStaticCtor(ctx.Graphics);
        ctx.Assets.Initialize();
        ctx.Assets.StartLoader(ctx.Graphics);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnLoadAssets(EngineSetupCtx ctx)
    {
        if (!ctx.Assets.ProcessLoader()) return false;
        ctx.Assets.FinishLoading();
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnSetupRender(EngineSetupCtx ctx)
    {
        RegisterCoreShaders(AssetManager.Assets);
        var builder = ctx.Renderer.Program.StartBuilder(EngineWindow.Viewport.Size);

        RegisterFrameBuffers(builder);
        builder.SetupPassPipeline(RenderPipelineVersion.Default3D);
        ctx.Renderer.Program.ApplyBuilder(builder);

        ctx.Renderer.Initialize();

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnSetupInternal(EngineSetupCtx ctx)
    {
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnLoadWorld(EngineSetupCtx ctx)
    {
        ctx.SceneSystem.QueueSwitch(0);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnLoadScene(EngineSetupCtx ctx)
    {
        var builder = new GameSceneConfigBuilder();
        ctx.SceneSystem.ApplyPendingScene(builder);
        ctx.SceneSystem.SetEnabled(true);

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnLoadEditor(EngineSetupCtx ctx)
    {
        ctx.EngineGateway.SetupEditor(ctx.CommandBus, ctx.Graphics.Gfx);
        Logger.ToggleGfxLog(true);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnWarmup(EngineSetupCtx ctx)
    {
        ctx.Graphics.BeginFrame(EngineWindow.Viewport.Size);
        ctx.Renderer.Program.Render();
        ctx.Graphics.EndFrame();
        ctx.EngineGateway.RenderEditor(0);

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnDone(EngineSetupCtx ctx)
    {
        return true;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void RegisterFrameBuffers(RenderSetupBuilder builder)
    {
        builder.RegisterFbo<ShadowPassTag>(FboVariant.V0,
            new RegisterFboEntry().AttachDepthTexture(FboDepthAttachment.Default())
                .UseFixedSize(new Size2D(VisualManager.Instance.Shadow.ShadowMapSize)));

        builder.RegisterFbo<ScenePassTag>(FboVariant.V0,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.Off(), RenderBufferMsaa.X4)
                .AttachDepthStencilBuffer());

        builder.RegisterFbo<ScenePassTag>(FboVariant.V1,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.DefaultMip())
                .AttachDepthStencilBuffer());

        builder.RegisterFbo<PostPassTag>(FboVariant.V0,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.Default()));

        builder.RegisterFbo<PostPassTag>(FboVariant.V1,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.Default()));

        builder.RegisterFbo<OutputPassTag>(FboVariant.V0,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.Default()));
    }

    internal static void RegisterCoreShaders(AssetStore store)
    {
        RenderRegistry.DepthShader = store.GetByName<Shader>("Depth").GfxId;
        RenderRegistry.ColorFilterShader = store.GetByName<Shader>("ColorFilter").GfxId;
        RenderRegistry.CompositeShader = store.GetByName<Shader>("Composite").GfxId;
        RenderRegistry.PresentShader = store.GetByName<Shader>("Present").GfxId;
        RenderRegistry.HighlightShader = store.GetByName<Shader>("Highlight").GfxId;
        RenderRegistry.BoundingBoxShader = store.GetByName<Shader>("BoundingBox").GfxId;
    }
}
