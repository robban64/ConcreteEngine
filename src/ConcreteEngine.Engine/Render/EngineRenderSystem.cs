using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render;

public sealed class EngineRenderSystem : RenderSystem, IGameEngineSystem
{
    internal RenderProgram Program { get; }

    private readonly CameraSystem _cameraSystem;
    private readonly VisualManager _visualManager;

    private readonly MaterialProcessor _materialProcessor;
    private readonly RenderDispatcher _renderDispatcher;

    internal EngineRenderSystem(GraphicsRuntime graphics, AssetStore assetStore)
    {
        _cameraSystem = CameraSystem.Instance;
        _visualManager = VisualManager.Instance;
        _visualManager.Shadow.ShadowMapSize = EngineSettings.Current.Graphics.ShadowSize;

        TerrainSystem.Make(graphics.Gfx);
        var particles = ParticleSystem.Make(graphics.Gfx);
        var animations = AnimationTable.Make();

        _renderDispatcher = new RenderDispatcher(animations, particles);
        _materialProcessor = new MaterialProcessor(assetStore);

        Program = new RenderProgram(graphics, VisualUniformProcessor.MakeCallbacks());
    }

    public override int VisibleCount => _renderDispatcher.VisibleCount;
    public override ReadOnlySpan<RenderEntityId> VisibleEntities() => _renderDispatcher.GetVisibleEntities();


    internal void Initialize(AssetStore assetStore, MaterialStore materialStore)
    {
        AnimationTable.Instance.Setup(assetStore);
        _renderDispatcher.Attach(Program.UploadBuffers);

        //
        var mat = materialStore.CreateMaterial("EmptyMat", "EmptyMat1");
        mat.Pipeline = new MaterialPipeline
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend,
                GfxStateFlags.DepthWrite | GfxStateFlags.Ac2),
            PassFunctions = new GfxPassFunctions(BlendMode.Alpha)
        };

        DrawTagProcessor.BoundsMaterial = mat.MaterialId;
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
    }


    internal void Render(float dt, Size2D viewportSize, Vector2 mousePos)
    {
        Program.PrepareFrame();

        if (_visualManager.HasPendingFrameBufferResize)
            Program.ResizeFrameBuffers(viewportSize, _visualManager.Shadow.ShadowMapSize);

        // frame update
        _cameraSystem.CommitFrame(EngineTime.GameAlpha);
        _materialProcessor.SubmitMaterialData(Program);

        // process and upload draw commands
        _renderDispatcher.Execute();

        // prepare buffers
        Program.CollectDrawBuffers();

        // upload buffers to gpu
        VisualUniformProcessor.Upload(Program.GetUploadContext(), viewportSize, mousePos);

        Program.UploadUniforms();
        Program.Render();

        VisualManager.Instance.ClearDirty();
    }

    public void Shutdown() => _renderDispatcher.Dispose();
}