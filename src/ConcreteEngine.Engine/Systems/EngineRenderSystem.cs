using System.Diagnostics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics.Animations;
using ConcreteEngine.Core.Engine.Graphics.Visuals;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Systems;

public sealed class EngineRenderSystem : IDisposable
{
    private readonly RenderResolver _resolver;

    private readonly MaterialSystem _materialSystem;
    private readonly TerrainSystem _terrainSystem;
    private readonly ParticleSystem _particleSystem;
    private readonly AnimationSystem _animationSystem;

    private readonly RenderRegistry _registry;
    private readonly RenderPassPipeline _passPipeline;
    private readonly DrawCommandPipeline _drawPipeline;

    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
        _ = CameraManager.Instance;
        _ = VisualManager.Instance;
        VisualManager.Instance.Shadow.ShadowMapSize = EngineSettings.Current.Graphics.ShadowSize;

        _materialSystem = new MaterialSystem();
        _terrainSystem = new TerrainSystem(graphics.Gfx);
        _particleSystem = new ParticleSystem(graphics.Gfx);
        _animationSystem = new AnimationSystem(AnimationManager.Instance);

        _registry = new RenderRegistry(graphics.Gfx);
        _drawPipeline = new DrawCommandPipeline(graphics.Gfx, _animationSystem, _materialSystem);
        _passPipeline = new RenderPassPipeline(_drawPipeline.DrawCmd, _registry);

        _resolver = new RenderResolver(CameraManager.Instance.Frustum);

        VisualSystem.Create(graphics.Gfx.Buffers);
    }

    internal void Init()
    {
        RegisterCoreShaders(AssetManager.Assets);
        PassPipeline.RegisterFrameBuffers(_registry);
        PassPipeline.RegisterPassPipeline(_passPipeline);
        VisualSystem.Instance.UploadPointLight();
    }

    internal void AfterUpdate()
    {
        VisualManager.Instance.Ensure();
        CameraManager.Instance.CommitUpdate();
        _materialSystem.Commit();
    }

    internal void OnSystemTick(bool screenResize)
    {
        _particleSystem.Commit();
        _terrainSystem.Commit();

        if (screenResize)
        {
            Logger.Log(LogScope.Engine, "Recreating screen framebuffers");
            _registry.RecreateScreenDependentFbo(EngineWindow.Viewport.Size);
            CameraManager.Instance.Camera.SetAspectRatio(EngineWindow.AspectRatio);
        }

        if (VisualManager.Instance.CommitShadowSize())
        {
            Logger.Log(LogScope.Engine, "Recreating shadow framebuffers");
            var size = new Size2D(VisualManager.Instance.Shadow.ShadowMapSize);
            _registry.RecreateFixedFrameBuffer<ShadowTarget>(FboVariant.V0, size);
        }
    }

    internal void OnSimulate(float dt)
    {
        _animationSystem.Simulate(dt);
        _particleSystem.Simulate(dt);
    }

    public void PrepareRenderer(float alpha)
    {
        _animationSystem.ResetFrame();
        _passPipeline.ResetFrame();
        _drawPipeline.ResetFrame();

        // frame update
        CameraManager.Instance.CommitFrame(alpha);
        VisualSystem.Instance.UploadUniforms();

        // process and upload draw commands
        _resolver.Execute();
        _particleSystem.Execute();
        _animationSystem.Execute(alpha);

        // prepare buffers
        VisualSystem.Instance.UploadUniformBuffers(_resolver, _materialSystem, _animationSystem);

        _drawPipeline.ReadyDrawCommands(_resolver.DrawIndices);
    }

    private static AvgFrameTimer avg;
    public void ExecuteRenderPipeline()
    {
        while (true)
        {
            avg.BeginSample();
            if (!_passPipeline.NextPass(out var result))
            {
                avg.EndSample();
                break;
            }
            if (result.NextAction == NextPassAction.Skip)
            {
                avg.EndSample();
                continue;
            }

            var passResult = _passPipeline.ApplyPass();
            avg.EndSample();
            if (passResult.Op is PassOp.Draw)
            {
                _drawPipeline.ExecuteDrawPass(result.Pass);
            }

            _passPipeline.ApplyAfterPass();
        }

        if (avg.Ticks > 80 * 10) avg.ResetAndPrint();
    }

    public void Dispose()
    {
        _resolver.Dispose();
        _particleSystem.Dispose();
        _animationSystem.Dispose();
        _materialSystem.Dispose();
    }

    private static void RegisterCoreShaders(AssetStore store)
    {
        RenderRegistry.DepthShader = store.GetByName<Shader>("Depth").GfxId;
        RenderRegistry.ColorFilterShader = store.GetByName<Shader>("ColorFilter").GfxId;
        RenderRegistry.CompositeShader = store.GetByName<Shader>("Composite").GfxId;
        RenderRegistry.PresentShader = store.GetByName<Shader>("Present").GfxId;
        RenderRegistry.HighlightShader = store.GetByName<Shader>("Highlight").GfxId;
        RenderRegistry.BoundingBoxShader = store.GetByName<Shader>("BoundingBox").GfxId;
    }
}