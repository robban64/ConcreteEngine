using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Editor;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Engine.Configuration.Setup;

internal sealed class EngineSetupCtx
{
    public required GraphicsRuntime Graphics;
    public required EngineWindow Window;
    public required EngineGateway EngineGateway;
    public required EngineCoreSystem CoreSystem;
    public required EngineCommandQueue CommandQueue;
    public required EngineTickHub TickHub;

    public AssetSystem Assets => CoreSystem.GetSystem<AssetSystem>();
    public EngineRenderSystem Renderer => CoreSystem.GetSystem<EngineRenderSystem>();
    public SceneSystem SceneSystem => CoreSystem.GetSystem<SceneSystem>();
    public InputSystem InputSystem => CoreSystem.GetSystem<InputSystem>();
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
    private static bool OnNotStarted( EngineSetupCtx ctx)
    {
        EngineWarmup.LoadStaticCtor(ctx.Graphics);
        ctx.Assets.Initialize();
        ctx.Assets.StartLoader(ctx.Graphics);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnLoadAssets( EngineSetupCtx ctx)
    {
        if (!ctx.Assets.ProcessLoader()) return false;
        ctx.Assets.FinishLoading();
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnSetupRender( EngineSetupCtx ctx)
    {
        var builder = ctx.Renderer.Program.StartBuilder(ctx.Window.WindowSize, ctx.Window.OutputSize);
        var shaderCount = ctx.Assets.Store.GetMetaSnapshot<Shader>().Count;

        builder.RegisterShader(shaderCount, ExtractShader).RegisterCoreShaders(GetCoreShaders);
        SetupUtils.RegisterFrameBuffers(builder);
        builder.SetupPassPipeline(RenderPipelineVersion.Default3D);
        ctx.Renderer.Program.ApplyBuilder(ctx.Assets.Store, builder);

        ctx.Renderer.Initialize(ctx.Assets.Store, ctx.Assets.MaterialStore);

        return true;

        static void ExtractShader(object objStore, Span<ShaderId> span)
        {
            var store = (AssetStore)objStore;
            var index = 0;
            foreach (var it in store.GetAssetEnumerator<Shader>())
                span[index++] = it.GfxId;
        }

        static RenderCoreShaders GetCoreShaders(object store) => SetupUtils.GetCoreShaders((AssetStore)store);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnSetupInternal( EngineSetupCtx ctx)
    {
        //EngineMetricHub.Attach(ctx.Assets.Store, ctx.SceneSystem.SceneManager, ctx.World);
        Ecs.Init();
        Logger.Setup();
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnLoadWorld( EngineSetupCtx ctx)
    {
        ctx.SceneSystem.QueueSwitch(0);
        CameraManager.Instance.AttachRaycast(ctx.SceneSystem.SceneManager, ctx.Renderer);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnLoadScene( EngineSetupCtx ctx)
    {
        var builder = new GameSceneConfigBuilder();
        ctx.SceneSystem.ApplyPendingScene(builder, ctx.CoreSystem);
        ctx.SceneSystem.SetEnabled(true);

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnLoadEditor( EngineSetupCtx ctx)
    {
        var apiContext = new ApiContext(ctx.Assets, ctx.SceneSystem.SceneManager);
        ctx.EngineGateway.SetupEditor(ctx.Window.PlatformWindow, ctx.InputSystem, ctx.Graphics.Gfx);
        ctx.EngineGateway.SetupEditorGateway(ctx.CommandQueue, apiContext);

        Logger.ToggleGfxLog(true);
        
        for(int i = 0; i < 3; i++) EngineWarmup.YeetGenerics(ctx.Graphics);
        
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnWarmup( EngineSetupCtx ctx)
    {
        ctx.Graphics.BeginFrame(new GfxFrameArgs(0, ctx.Window.OutputSize));
        ctx.Renderer.Program.PrepareFrameWarmup(ctx.Window.WindowSize, ctx.Window.OutputSize);

        ctx.Renderer.Program.Render();

        ctx.Graphics.EndFrame();

        ctx.EngineGateway.RenderEditor(0, ctx.Window.WindowSize);

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OnDone( EngineSetupCtx ctx)
    {
        return true;
    }
}

file static class SetupUtils
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void RegisterFrameBuffers(RenderSetupBuilder builder)
    {
        builder.RegisterFbo<ShadowPassTag>(FboVariant.Default,
            new RegisterFboEntry().AttachDepthTexture(FboDepthAttachment.Default())
                .UseFixedSize(new Size2D(VisualManager.Instance.VisualEnv.GetShadow().ShadowMapSize)));

        builder.RegisterFbo<ScenePassTag>(FboVariant.Default,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.Off(), RenderBufferMsaa.X4)
                .AttachDepthStencilBuffer());

        builder.RegisterFbo<ScenePassTag>(FboVariant.Secondary,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.DefaultMip())
                .AttachDepthStencilBuffer());

        builder.RegisterFbo<PostPassTag>(FboVariant.Default,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.Default()));

        builder.RegisterFbo<PostPassTag>(FboVariant.Secondary,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.Default()));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static RenderCoreShaders GetCoreShaders(AssetStore store) =>
        new()
        {
            DepthShader = store.GetByName<Shader>("Depth").GfxId,
            ColorFilterShader = store.GetByName<Shader>("ColorFilter").GfxId,
            CompositeShader = store.GetByName<Shader>("Composite").GfxId,
            PresentShader = store.GetByName<Shader>("Present").GfxId,
            HighlightShader = store.GetByName<Shader>("Highlight").GfxId,
            BoundingBoxShader = store.GetByName<Shader>("BoundingBox").GfxId,
            ParticleShader = store.GetByName<Shader>("Particle").GfxId,
        };
}