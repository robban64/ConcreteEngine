using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics.Animations;
using ConcreteEngine.Core.Engine.Graphics.Particles;
using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Core.Engine.Render;
using ConcreteEngine.Core.Engine.Render.Systems;
using ConcreteEngine.Engine.RenderPipeline;
using ConcreteEngine.Engine.RenderPipeline.Impl;
using ConcreteEngine.Graphics;
using RenderStore = ConcreteEngine.Engine.RenderPipeline.RenderStore;

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


    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
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
        _transformBuffer = new RenderTransformBuffer();
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
        AnimationManager.Instance.Simulate(dt);
        ParticleManager.Instance.Simulate((float)dt);
    }


    private bool abc = false;
    public void PrepareRenderer()
    {
        RenderContext.ResetContext();
        _animationSystem.ResetFrame();
        _drawCmd.ResetFrame();
        _passContext.ResetFrame();

        // frame update
        _renderCamera.UpdateFrame(EngineTime.GameAlphaF);

        // process and upload draw commands
        RenderWorld.System<RenderCullSystem>().Execute(EngineTime.FrameId, _renderCamera);
        RenderWorld.System<RenderPassSystem>().Execute(EngineTime.FrameId);

        
        TestAll();
        abc = true;

        

        _transformBuffer.Execute();
        _particleSystem.Execute();
        _animationSystem.Execute(EngineTime.GameAlpha);

        // prepare buffers
        VisualSystem.Instance.UploadUniforms();
        VisualSystem.Instance.UploadUniformBuffers(_transformBuffer, _materialSystem, _animationSystem);
    }

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
        var sources = RenderWorld.Dense<DrawSource>().AsView();
        var tickets = RenderWorld.System<RenderPassSystem>().GetDrawTickets(passId);
        foreach (var ticket in tickets)
        {
            ref readonly var source = ref sources[ticket.Entity];
            _drawCmd.DrawSource(source, ticket);
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
    
    
    private AvgFrameTimer avg1, avg2;

    private void TestAll()
    {
        avg1.BeginSample();
        Test();
        avg1.EndSample();
        avg2.BeginSample();
        Test1();
        avg2.EndSample();

        if (entity1 != entity2)
            throw new InvalidOperationException($"Entity: {entity1} - {entity2}; Iter: {iteration1} - {iteration2}");
        if (iteration1 != iteration2)
            throw new InvalidOperationException($"Iter: {iteration1} - {iteration2}");


        if (avg1.Ticks > 100)
        {
            avg1.ResetAndPrint("Test0");
            avg2.ResetAndPrint("Test1");
        }
    }


    private static int iteration1, iteration2, entity1, entity2;
    private static int Test()
    {
        iteration1 = 0;
        var x = 0;
        foreach (var filter in RenderWorld.Query.New<DrawPolicy, DrawSource>()
                     .Filter(RenderWorld.Meta.EntitySet, RenderWorld.Meta.VisibleSet))
        {
            var xx = 0;
            foreach (var query in filter)
            {
                entity1 = query.Entity;
                xx += (int)query.Component1.Passes + query.Component2.MeshIndex;
                ++iteration1;
            }

            x += xx;
        }

        return x;
    }

    private static void Test1()
    {
        iteration2 = 0;
        foreach (var filter in RenderWorld.Query.New<DrawPolicy>()
                     .Filter(RenderWorld.Meta.EntitySet, RenderWorld.Meta.VisibleSet))
        {
            foreach (var query in filter)
            {
                entity2 = query.Entity;
                ++iteration2;
            }
        }
    }


}