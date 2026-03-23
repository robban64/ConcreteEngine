using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render;

public sealed class EngineRenderSystem : GameEngineSystem
{

    private readonly RenderProgram _renderer;
    private readonly FrameProcessor _frameProcessor;
    private readonly RenderDispatcher _renderDispatcher;

    private readonly CameraManager _cameraManager;
    private readonly VisualManager _visualManager;
    
    internal readonly Skybox Sky;
    internal readonly TerrainManager Terrain;
    internal readonly ParticleManager Particles;
    internal readonly AnimationTable Animations;


    internal EngineRenderSystem(GraphicsRuntime graphics,  MaterialStore materialStore)
    {
        _cameraManager = CameraManager.Instance;
        _visualManager = VisualManager.Instance;
        
        Sky = Skybox.Instance;
        Terrain = new TerrainManager(graphics.Gfx);
        Particles = new ParticleManager(graphics.Gfx);
        Animations = new AnimationTable();
        
        _renderDispatcher = new RenderDispatcher(Animations,Particles);
        _frameProcessor = new FrameProcessor(materialStore);
        
        _renderer = new RenderProgram(graphics, _cameraManager.Camera, _visualManager.VisualEnv);

    }

    internal RenderProgram Program => _renderer;

    internal int VisibleCount => _renderDispatcher.VisibleCount;
    internal ReadOnlySpan<RenderEntityId> VisibleEntities() => _renderDispatcher.GetVisibleEntities();

    internal void Initialize(AssetStore assetStore, MaterialStore materialStore)
    {
        Animations.Setup(assetStore);
        _renderDispatcher.Init(_renderer.CommandBuffer);
        
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
    internal void BeforeUpdate(Size2D outputSize)
    {
        _cameraManager.Camera.BeginUpdate(outputSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AfterUpdate()
    {
        _visualManager.UpdateToCamera(_cameraManager.Camera);
        Terrain.Update();
    }

    internal void Render(in RenderFrameArgs args)
    {
        _renderer.PrepareFrame(in args);
        
        
        // frame update
        _cameraManager.Camera.UpdateFrameView(args.Alpha);
        _frameProcessor.SubmitMaterialData(_renderer);
        _frameProcessor.Execute(args.DeltaTime, args.Alpha);
        
        // process and upload draw commands
        _renderDispatcher.Execute();

        // prepare buffers
        _renderer.CollectDrawBuffers();

        // upload buffers to gpu
        _renderer.UploadFrameData();

        _renderer.Render();
    }



}