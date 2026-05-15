using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render;

public sealed class EngineRenderSystem : RenderSystem, IGameEngineSystem
{
    internal RenderProgram Program { get; }

    private readonly FrameProcessor _frameProcessor;
    private readonly RenderDispatcher _renderDispatcher;

    private readonly VisualManager _visualManager;
    private readonly VisualUniformProcessor _uniformProcessor;

    private readonly CameraSystem _cameraSystem;
    private readonly RenderObjectManager _renderObjectManager;

    internal EngineRenderSystem(GraphicsRuntime graphics, MaterialStore materialStore)
    {
        _cameraSystem = CameraSystem.Instance;
        _visualManager = VisualManager.Instance;
        _visualManager.Shadow.ShadowMapSize = EngineSettings.Current.Graphics.ShadowSize;
        _uniformProcessor = new VisualUniformProcessor(_visualManager);

        _renderObjectManager = new RenderObjectManager(graphics);

        _renderDispatcher = new RenderDispatcher(Animations, Particles);
        _frameProcessor = new FrameProcessor(materialStore);

        Program = new RenderProgram(graphics,
            new UniformUploaderCallbacks
            {
                UploadMainView = VisualUniformProcessor.UploadMainView,
                UploadLightView = VisualUniformProcessor.UploadLightView,
                UploadShadow = VisualUniformProcessor.UploadShadow
            });
    }

    internal TerrainSystem Terrains => _renderObjectManager.TerrainSystem;
    internal ParticleSystem Particles => _renderObjectManager.Particles;
    internal AnimationTable Animations => _renderObjectManager.Animations;

    public override Terrain Terrain => Terrains.Terrain;
    public override int VisibleCount => _renderDispatcher.VisibleCount;
    public override ReadOnlySpan<RenderEntityId> VisibleEntities() => _renderDispatcher.GetVisibleEntities();


    internal void Initialize(AssetStore assetStore, MaterialStore materialStore)
    {
        Animations.Setup(assetStore);
        _renderDispatcher.Attach(Program.UploadBuffers);
        _uniformProcessor.Attach(Program.UniformUploader);

        //
        var mat = materialStore.CreateMaterial("EmptyMat", "EmptyMat1");
        mat.Pipeline = new MaterialPipeline
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend,
                GfxStateFlags.DepthWrite | GfxStateFlags.SampleAlphaCoverage),
            PassFunctions = new GfxPassFunctions(BlendMode.Alpha)
        };
        
        DrawTagResolver.BoundsMaterial = mat.MaterialId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BeforeUpdate()
    {
        _cameraSystem.BeginUpdate();
    }

    internal void AfterUpdate()
    {
        _visualManager.Ensure();
        _cameraSystem.CommitUpdate(_visualManager);
        Terrains.Update();
    }


    internal void Render(float dt, Size2D viewportSize, Vector2 mousePos)
    {
        Program.PrepareFrame();

        if (_visualManager.HasPendingFrameBufferResize)
            Program.ResizeFrameBuffers(viewportSize, _visualManager.Shadow.ShadowMapSize);

        // frame update
        _cameraSystem.CommitFrame(EngineTime.GameAlpha);
        _frameProcessor.SubmitMaterialData(Program);
        _frameProcessor.Execute(dt, EngineTime.GameAlpha);

        // process and upload draw commands
        _renderDispatcher.Execute();

        // prepare buffers
        Program.CollectDrawBuffers();

        // upload buffers to gpu
        _uniformProcessor.Upload(viewportSize, mousePos);
        Program.UploadUniforms();

        Program.Render();

        VisualManager.Instance.ClearDirty();
    }

    public void Shutdown() => _renderDispatcher.Dispose();
}