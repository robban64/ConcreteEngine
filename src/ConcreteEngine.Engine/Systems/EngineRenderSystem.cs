using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.Graphics.Animations;
using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Render.Impl;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Systems;

public sealed class EngineRenderSystem : IDisposable
{
    private readonly Camera _renderCamera;
    private readonly RenderTransformBuffer _transformBuffer;
    private readonly DrawCommandProcessor _drawCmd;
    private readonly RenderPassContext _passContext;

    private readonly MaterialSystem _materialSystem;
    private readonly TerrainSystem _terrainSystem;
    private readonly ParticleSystem _particleSystem;
    private readonly AnimationSystem _animationSystem;

    private readonly RenderEntityCore _renderEcs;

    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
        _renderEcs = RenderEcs.Core ?? throw new InvalidOperationException();
        
        _renderCamera = new Camera(EngineSettings.Current.Display.WindowSize);
        _ = VisualManager.Instance;
        VisualManager.Instance.Lighting.Shadow.ShadowMapSize = EngineSettings.Current.Graphics.ShadowSize;

        RenderRegistry.Create(graphics.Gfx);
        VisualSystem.Create(graphics.Gfx.Buffers);

        _materialSystem = new MaterialSystem();
        _terrainSystem = new TerrainSystem(graphics.Gfx);
        _particleSystem = new ParticleSystem(graphics.Gfx);
        _animationSystem = new AnimationSystem(AnimationManager.Instance);

        _drawCmd = new DrawCommandProcessor(graphics.Gfx, _animationSystem, _materialSystem);
        _passContext = new RenderPassContext(_drawCmd);
        _transformBuffer = new RenderTransformBuffer(RenderEcs.Core.Data);

    }

    internal void Init()
    {
        RegisterCoreShaders(AssetManager.Assets);
        PassPipeline.RegisterFrameBuffers();
        PassPipeline.RegisterPassPipeline();
        VisualSystem.Instance.UploadPointLight();
    }
    
    internal void AfterUpdate()
    {
        var visuals = VisualManager.Instance;
        visuals.Commit();
        
        _renderCamera.Commit(visuals.Lighting);
        _materialSystem.Commit();
    }

    internal void OnSystemTick(bool screenResize)
    {
        _particleSystem.Commit();
        _terrainSystem.Commit();

        if (screenResize)
        {
            Logger.Log(LogScope.Engine, "Recreating screen framebuffers");
            RenderRegistry.Instance.RecreateScreenDependentFbo(EngineWindow.Viewport.Size);
            _renderCamera.SetAspectRatio(EngineWindow.AspectRatio);
        }

        if (VisualManager.Instance.CommitShadowSize())
        {
            Logger.Log(LogScope.Engine, "Recreating shadow framebuffers");
            var size = new Size2D(VisualManager.Instance.Lighting.Shadow.ShadowMapSize);
            RenderRegistry.Instance.RecreateFixedFrameBuffer<ShadowTarget>(FboVariant.V0, size);
        }
    }

    internal void OnSimulate(double dt)
    {
        _animationSystem.Simulate(dt);
        _particleSystem.Simulate((float)dt);
    }
    
    public void PrepareRenderer()
    {
        RenderContext.ResetContext();
        _animationSystem.ResetFrame();
        _drawCmd.ResetFrame();
        _passContext.ResetFrame();

        // frame update
        _renderCamera.UpdateFrame(EngineTime.GameAlphaF);


        // process and upload draw commands
        _renderEcs.CullSystem.Execute(_renderCamera.FrameTransforms, _renderCamera.LightTransforms);
        _renderEcs.PassSystem.Execute();
        avg.BeginSample();
        _transformBuffer.Execute();
        if (avg.EndSample() > 100) avg.ResetAndPrint();

        _particleSystem.Execute();
        _animationSystem.Execute(EngineTime.GameAlpha);

        // prepare buffers
        VisualSystem.Instance.UploadUniforms();
        VisualSystem.Instance.UploadUniformBuffers(_transformBuffer, _materialSystem, _animationSystem);
    }

    private AvgFrameTimer avg;

    public void ExecuteRenderPipeline()
    {
        var length = RenderRegistry.PassCount;
        for (var i = 0; i < length; ++i)
        {
            var passResult = BeginPass(i);

            if (passResult.Op is PassOp.Draw)
            {
                _drawCmd.PrepareDrawPass();
                ExecuteDrawPass(i);
            }

            EndPass(i);
        }

    }

    private void ExecuteDrawPass(int passId)
    {
        var tickets = _renderEcs.PassSystem.GetDrawTickets(passId);
        foreach (ref readonly var ticket in tickets)
        {
            //TODO
            var ctx = RenderEcs.Core.GetDrawContext(ticket.Entity);
            _drawCmd.DrawSource(ctx, ticket.SubmitIndex);
        }
    }

    private PassAction BeginPass(int passId)
    {
        var passEntry = RenderRegistry.GetPassEntry(new PassId(passId));
        _passContext.AttachPass(passEntry);
        return passEntry.BeginPassDel(_passContext);
    }

    private void EndPass(int passId)
    {
        var passEntry = RenderRegistry.GetPassEntry(new PassId(passId));
        passEntry.EndPassDel?.Invoke(_passContext);
    }

    public void Dispose()
    {
        _transformBuffer.Dispose();
        _particleSystem.Dispose();
        _animationSystem.Dispose();
        _materialSystem.Dispose();
    }

    private static void RegisterCoreShaders(AssetStore store)
    {
        RenderStore.DepthShader = store.GetByName<Shader>("Depth").GfxId;
        RenderStore.ColorFilterShader = store.GetByName<Shader>("ColorFilter").GfxId;
        RenderStore.CompositeShader = store.GetByName<Shader>("Composite").GfxId;
        RenderStore.PresentShader = store.GetByName<Shader>("Present").GfxId;
        RenderStore.HighlightShader = store.GetByName<Shader>("Highlight").GfxId;
        RenderStore.BoundingBoxShader = store.GetByName<Shader>("BoundingBox").GfxId;
    }
}