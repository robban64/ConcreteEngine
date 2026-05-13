using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Render;

public sealed class EngineRenderSystem : RenderSystem, IGameEngineSystem
{
    internal RenderProgram Program { get; }

    private readonly EngineWindow _window;
    private readonly FrameProcessor _frameProcessor;
    private readonly RenderDispatcher _renderDispatcher;

    private readonly GlobalVisualSettings _visualSettings;
    private readonly VisualUniformProcessor _uniformProcessor;

    private readonly CameraManager _cameraManager;
    private readonly RenderObjectManager _renderObjectManager;

    internal EngineRenderSystem(EngineWindow window, GraphicsRuntime graphics, MaterialStore materialStore)
    {
        _window = window;
        _cameraManager = CameraManager.Instance;
        _renderObjectManager = new RenderObjectManager(graphics);

        _renderDispatcher = new RenderDispatcher(Animations, Particles);
        _frameProcessor = new FrameProcessor(materialStore);

        _visualSettings = GlobalVisualSettings.Instance;
        _visualSettings.Shadow.ShadowMapSize = EngineSettings.Instance.Graphics.ShadowSize;
        _uniformProcessor = new VisualUniformProcessor(_visualSettings);

        Program = new RenderProgram(graphics,
            new UniformUploaderCallbacks
            {
                UploadMainView = VisualUniformProcessor.UploadMainView,
                UploadLightView = VisualUniformProcessor.UploadLightView,
                UploadShadow = VisualUniformProcessor.UploadShadow
            });
    }

    internal TerrainManager Terrains => _renderObjectManager.TerrainManager;
    internal ParticleManager Particles => _renderObjectManager.Particles;
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
        _cameraManager.Camera.BeginUpdate(_window.Viewport.Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AfterUpdate()
    {
        _visualSettings.Ensure();
        _cameraManager.Update(_visualSettings);
        Terrains.Update();
    }


    internal void Render(float dt, Size2D viewportSize, Vector2 mousePos)
    {
        Program.PrepareFrame();

        if (_visualSettings.HasPendingFrameBufferResize)
            Program.ResizeFrameBuffers(viewportSize, _visualSettings.Shadow.ShadowMapSize);

        // frame update
        _cameraManager.UpdateFrameView(EngineTime.GameAlpha);
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

        GlobalVisualSettings.Instance.ClearDirty();
    }

    public void Shutdown() => _renderDispatcher.Dispose();
}