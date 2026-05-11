using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer;

namespace ConcreteEngine.Engine.Render;

public sealed class EngineRenderSystem : RenderSystem, IGameEngineSystem
{
    internal RenderProgram Program { get; }

    private readonly EngineWindow _window;
    private readonly FrameProcessor _frameProcessor;
    private readonly RenderDispatcher _renderDispatcher;

    private readonly CameraManager _cameraManager;
    private readonly VisualManager _visualManager;

    internal readonly TerrainManager TerrainManager;
    internal readonly ParticleManager Particles;
    internal readonly AnimationTable Animations;


    internal EngineRenderSystem(EngineWindow window, GraphicsRuntime graphics, MaterialStore materialStore)
    {
        _window = window;
        _cameraManager = CameraManager.Instance;
        _visualManager = VisualManager.Instance;

        TerrainManager = new TerrainManager(graphics.Gfx);
        Particles = new ParticleManager(graphics.Gfx);
        Animations = new AnimationTable();

        _renderDispatcher = new RenderDispatcher(Animations, Particles);
        _frameProcessor = new FrameProcessor(materialStore);

        Program = new RenderProgram(graphics, _cameraManager.Transforms, _visualManager.VisualEnv);
    }

    public override Terrain Terrain => TerrainManager.Terrain;
    public override int VisibleCount => _renderDispatcher.VisibleCount;
    public override ReadOnlySpan<RenderEntityId> VisibleEntities() => _renderDispatcher.GetVisibleEntities();


    internal void Initialize(AssetStore assetStore, MaterialStore materialStore)
    {
        Animations.Setup(assetStore);
        _renderDispatcher.Init(Program.UploadBuffers);

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
        _cameraManager.UpdateLightView(_visualManager.VisualEnv);
        TerrainManager.Update();
    }


    internal void Render(float dt, Size2D viewportSize, Vector2 mousePos)
    {
        Program.PrepareFrame(dt, viewportSize);

        // frame update
        _cameraManager.UpdateFrameView(EngineTime.GameAlpha);
        _frameProcessor.SubmitMaterialData(Program);
        _frameProcessor.Execute(dt, EngineTime.GameAlpha);

        // process and upload draw commands
        _renderDispatcher.Execute();

        // prepare buffers
        Program.CollectDrawBuffers();

        // upload buffers to gpu
        Program.UploadFrameData(new RenderFrameArgs(mousePos, EngineTime.Time, EngineTime.FrameRng));

        Program.Render();
    }

    public void Shutdown() => _renderDispatcher.Dispose();
}