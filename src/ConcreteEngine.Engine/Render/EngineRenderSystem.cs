using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render;

public sealed class EngineRenderSystem
{
    private DrawCommandBuffer _commandBuffer = null!;
    private MaterialStore _materialStore = null!;

    private readonly RenderProgram _renderer;
    private readonly RenderDispatcher _renderDispatcher;
    private readonly FrameProcessor _frameProcessor;

    private readonly CameraTransform _camera;
    
    private bool _hasUploadedMaterial;
    internal AnimationTable Animations { get; }
    internal readonly Skybox Sky;
    internal readonly Terrain Terrain;
    internal readonly ParticleSystem Particles;

    public readonly MeshGeneratorRegistry MeshGenerator;


    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
        _camera = CameraSystem.Instance.Camera;
        _renderer = new RenderProgram(graphics, _camera, VisualSystem.Instance.VisualEnv);
        Animations = new AnimationTable();
        _renderDispatcher = new RenderDispatcher(Ecs.Render.Core);
        _frameProcessor = new FrameProcessor();
        
        MeshGenerator = new MeshGeneratorRegistry();
        Sky = new Skybox();
        Terrain = new Terrain();
        Particles = new ParticleSystem();

    }

    internal RenderProgram Program => _renderer;

    internal int VisibleCount => _renderDispatcher.VisibleCount;
    internal ReadOnlySpan<RenderEntityId> VisibleEntities() => _renderDispatcher.GetVisibleEntities();

    internal void Initialize(GfxContext gfx, AssetStore assetStore, MaterialStore materialStore)
    {
        _materialStore = materialStore;
        _commandBuffer = _renderer.CommandBuffer;
        Animations.Setup(assetStore);

        _renderDispatcher.Init(Animations,Sky,Particles, _commandBuffer);
        
        //
        MeshGenerator.Register(new TerrainMeshGenerator(gfx, Terrain));
        Particles.AttachRenderer(MeshGenerator.Register(new ParticleMeshGenerator(gfx)));

        RenderDispatcher.TerrainMesh = MeshGenerator.Get<TerrainMeshGenerator>();

        var mat = materialStore.CreateMaterial("EmptyMat", "EmptyMat1");
        mat.Pipeline = new MaterialPipeline
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend,
                GfxStateFlags.DepthWrite | GfxStateFlags.SampleAlphaCoverage),
            PassFunctions = new GfxPassFunctions(BlendMode.Alpha)
        };


        DrawTagResolver.BoundsMaterial = mat.MaterialId;

    }

    internal void Render(in RenderFrameArgs args)
    {
        _renderer.PrepareFrame(in args);
        
        _camera.UpdateFrameView(args.Alpha);

        SubmitMaterialData();
        EnsureCommandBuffer();
        
        // frame update
        _frameProcessor.Execute(args.DeltaTime, args.Alpha);
        
        // process and upload draw commands
        _renderDispatcher.Execute();

        // prepare buffers
        _renderer.CollectDrawBuffers();

        // upload buffers to gpu
        _renderer.UploadFrameData();

        _renderer.Render();
    }

    private void SubmitMaterialData()
    {
        if (!_materialStore.HasDirtyMaterials && _hasUploadedMaterial) return;
        if (_materialStore.HasDirtyMaterials) _hasUploadedMaterial = false;

        _materialStore.ClearDirtyMaterials();

        Span<TextureBinding> slots = stackalloc TextureBinding[RenderLimits.TextureSlots];
        foreach (var material in _materialStore.GetMaterials())
        {
            int slotLength = _materialStore.GetMaterialUploadData(material!, slots, out var payload);
            _renderer.SubmitMaterialDrawData(in payload, slots.Slice(0, slotLength));
        }

        _hasUploadedMaterial = true;
    }

    private void EnsureCommandBuffer()
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityLen = Ecs.Render.Core.Count + extraEntities;
        var animationLen = Ecs.Render.Stores<RenderAnimationComponent>.Store.Count + extraAnimations;

        _commandBuffer.EnsureBufferCapacity(entityLen);
        _commandBuffer.EnsureBoneBuffer(animationLen);
    }
}