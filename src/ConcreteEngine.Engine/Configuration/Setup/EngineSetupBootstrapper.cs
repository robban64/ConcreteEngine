using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Editor;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Configuration.Setup;

internal sealed class AssetGfxSetupContext
{
    public required AssetSystem Assets;
    public required GraphicsRuntime Graphics;
}

internal sealed class MainSetupContext
{
    public required AssetStore AssetStore;
    public required SceneSystem SceneSystem;
    public required World World;
    public required EngineGateway EngineGateway;
    public required EngineCoreSystem CoreSystem;
    public required EngineCommandQueue CommandQueue;
}

internal sealed class RendererSetupContext
{
    public required RenderEngine Renderer;
    public required AssetStore AssetStore;
    public required WorldVisual WorldVisual;
    public required EngineWindow Window;
}

internal sealed class EngineSetupCtx
{
    public required AssetSystem Assets;
    public required GraphicsRuntime Graphics;
    public required RenderEngine Renderer;
    public required World World;
    public required EngineWindow Window;
    public required EngineGateway EngineGateway;
    public required EngineCoreSystem CoreSystem;
    public required EngineCommandQueue CommandQueue;
    public required SceneSystem SceneSystem;
    public required InputSystem InputSystem;
}

internal static class EngineSetupBootstrapper
{
    public static void RegisterSteps(EngineSetupPipeline pipeline, EngineSetupCtx ctx)
    {
        /*
        var mainCtx = new MainSetupContext
        {
            AssetStore = ctx.Assets.Store,
            CommandQueue = ctx.CommandQueue,
            CoreSystem = ctx.CoreSystem,
            EngineGateway = ctx.EngineGateway,
            SceneManager = ctx.SceneManager,
            World = ctx.World
        };
        var assetCtx = new AssetGfxSetupContext { Assets = ctx.Assets, Graphics = ctx.Graphics };
        var renderCtx = new RendererSetupContext
        {
            AssetStore = ctx.Assets.Store,
            Renderer = ctx.Renderer,
            Window = ctx.Window,
            WorldVisual = ctx.World.WorldVisual
        };
*/
        pipeline.RegisterStep(EngineSetupState.NotStarted, ctx, OnNotStarted);
        pipeline.RegisterStep(EngineSetupState.LoadAssets, ctx, OnLoadAssets);
        pipeline.RegisterStep(EngineSetupState.SetupRenderer, ctx, OnSetupRender);
        pipeline.RegisterStep(EngineSetupState.SetupInternal, ctx, OnSetupInternal);
        pipeline.RegisterStep(EngineSetupState.LoadWorld, ctx, OnLoadWorld);
        pipeline.RegisterStep(EngineSetupState.LoadScene, ctx, OnLoadScene);
        pipeline.RegisterStep(EngineSetupState.LoadEditor, ctx, OnLoadEditor);
        pipeline.RegisterRunner(EngineSetupState.Warmup, 144, ctx, OnWarmup);
        pipeline.RegisterStep(EngineSetupState.Final, ctx, OnDone);
    }

    private static bool OnNotStarted(float dt, EngineSetupCtx ctx)
    {
        ctx.Assets.Initialize();
        ctx.Assets.StartLoader(ctx.Graphics.Gfx);
        return true;
    }

    private static bool OnLoadAssets(float dt, EngineSetupCtx ctx)
    {
        if (!ctx.Assets.ProcessLoader()) return false;
        ctx.Assets.FinishLoading();
        return true;
    }

    private static bool OnSetupRender(float dt, EngineSetupCtx ctx)
    {
        var builder = ctx.Renderer.StartBuilder(ctx.Window.WindowSize, ctx.Window.OutputSize);
        var shaderCount = ctx.Assets.Store.GetMetaSnapshot<Shader>().Count;

        builder.RegisterShader(shaderCount, ExtractShaderIds).RegisterCoreShaders(GetCoreShaders);
        WorldRenderSetup.RegisterFrameBuffers(builder, ctx.World.WorldVisual);
        builder.SetupPassPipeline(RenderPipelineVersion.Default3D);
        ctx.Renderer.ApplyBuilder(ctx.Assets.Store, builder);
        return true;

        static void ExtractShaderIds(object store, Span<ShaderId> span) =>
            ((AssetStore)store).ExtractSpan<Shader, ShaderId>(span, static shader => shader.ShaderId);

        static RenderCoreShaders GetCoreShaders(object store) => WorldRenderSetup.GetCoreShaders((AssetStore)store);
    }

    private static bool OnSetupInternal(float dt, EngineSetupCtx ctx)
    {
        EngineMetricHub.Attach(ctx.Assets.Store, ctx.SceneSystem.Scene, ctx.World);
        Logger.Setup();
        return true;
    }

    private static bool OnLoadWorld(float dt, EngineSetupCtx ctx)
    {
        ctx.SceneSystem.QueueSwitch(0);
        ctx.World.Initialize(ctx.Assets, ctx.Graphics.Gfx);
        return true;
    }

    private static bool OnLoadScene(float dt, EngineSetupCtx ctx)
    {
        var builder = new GameSceneConfigBuilder();
        ctx.SceneSystem.ApplyPendingScene(builder, ctx.CoreSystem);
        ctx.SceneSystem.SetEnabled(true);
        return true;
    }


    private static bool OnLoadEditor(float dt, EngineSetupCtx ctx)
    {
        EngineWarmup.PreWarmup(ctx.Graphics);

        var apiContext = new ApiContext(ctx.World, ctx.Assets.Store, ctx.SceneSystem.Scene);
        ctx.EngineGateway.SetupEditor(ctx.Window.PlatformWindow, ctx.InputSystem);
        ctx.EngineGateway.SetupEditorGateway(ctx.CommandQueue, apiContext);

        Logger.ToggleGfxLog(true);

        return true;
    }

    private static bool OnWarmup(float dt, EngineSetupCtx ctx)
    {
        ctx.Graphics.BeginFrame(new GfxFrameArgs(0, ctx.Window.OutputSize));
        ctx.Renderer.PrepareFrameWarmup(ctx.Window.WindowSize, ctx.Window.OutputSize);

        ctx.World.PreRender();
        ctx.Renderer.Render();

        ctx.Graphics.EndFrame();

        ctx.EngineGateway.RenderEditor(dt, ctx.Window.WindowSize);

        return false;
    }

    private static bool OnDone(float dt, EngineSetupCtx ctx)
    {
        return true;
    }
}