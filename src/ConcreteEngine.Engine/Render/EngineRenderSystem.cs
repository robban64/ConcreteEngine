using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render;

public sealed class EngineRenderSystem
{
    private DrawCommandBuffer _commandBuffer = null!;
    private FrameProcessor _frameProcessor = null!;
    private MaterialStore _materialStore = null!;

    private readonly RenderProgram _renderer;
    private readonly RenderDispatcher _renderDispatcher;

    private bool _hasUploadedMaterial;

    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
        _renderer = new RenderProgram(graphics, CameraSystem.Instance.Camera, VisualSystem.Instance.VisualEnv);
        _renderDispatcher = new RenderDispatcher(Ecs.Render.Core);
    }

    internal RenderProgram Program => _renderer;

    internal int VisibleCount => _renderDispatcher.VisibleCount;
    internal ReadOnlySpan<RenderEntityId> VisibleEntities() => _renderDispatcher.GetVisibleEntities();

    internal void Initialize(MaterialStore materialStore, World world)
    {
        _materialStore = materialStore;
        _commandBuffer = _renderer.CommandBuffer;

        _frameProcessor = new FrameProcessor();
        _renderDispatcher.Init(world.Bundle, _commandBuffer);
    }

    internal void Render(in RenderFrameArgs args)
    {
        _renderer.PrepareFrame(in args);
        CameraSystem.Instance.Camera.UpdateFrameView(args.Alpha);

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