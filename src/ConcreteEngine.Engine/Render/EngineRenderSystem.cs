using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Engine.Render;

public sealed class EngineRenderSystem
{
    private DrawCommandBuffer _commandBuffer = null!;
    private FrameProcessor _frameProcessor = null!;
    private MaterialStore _materialStore = null!;

    private Camera _camera = null!;

    private readonly RenderEntityCore _ecs;
    private readonly RenderProgram _renderer;
    private readonly FrameEntityBuffer _frameBuffer;
    private readonly RenderDispatcher _renderDispatcher;

    private bool _hasUploadedMaterial;

    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
        _ecs = Ecs.Render.Core;
        _renderer = new RenderProgram(graphics, PrimitiveMeshes.FsqQuad);
        _frameBuffer = new FrameEntityBuffer();
        _renderDispatcher = new RenderDispatcher(_ecs, _frameBuffer);

        RuntimeHelpers.RunClassConstructor(typeof(AnimatorProcessor).TypeHandle);
    }

    internal RenderProgram Program => _renderer;
    internal FrameEntityBuffer FrameEntityBuffer => _frameBuffer;

    internal int VisibleCount => _frameBuffer.VisibleCount;
    internal ReadOnlySpan<RenderEntityId> VisibleEntities() => _frameBuffer.GetVisibleEntities();

    internal void Initialize(MaterialStore materialStore, World world)
    {
        _camera = world.Bundle.Camera;

        _materialStore = materialStore;
        _commandBuffer = _renderer.CommandBuffer;

        _frameProcessor = new FrameProcessor();
        _renderDispatcher.Init(world.Bundle, _commandBuffer);
    }

    internal void Render(in RenderFrameArgs args)
    {
        var renderer = _renderer;
        
        renderer.PrepareFrame(in args);
        _camera.WriteSnapshot(args.Alpha, renderer.RenderCamera);

        SubmitMaterialData();
        EnsureCommandBuffer();

        _frameBuffer.Prepare();

        _frameProcessor.Execute(args.DeltaTime, args.Alpha);
        _renderDispatcher.Execute();

        // prepare buffers
        renderer.CollectDrawBuffers();

        // upload buffers to gpu
        renderer.UploadFrameData();

        renderer.Render();
    }

    private void SubmitMaterialData()
    {
        var matStore = _materialStore;
        if (!matStore.HasDirtyMaterials && _hasUploadedMaterial) return;
        if (matStore.HasDirtyMaterials) _hasUploadedMaterial = false;

        matStore.ClearDirtyMaterials();

        Span<TextureBinding> slots = stackalloc TextureBinding[RenderLimits.TextureSlots];
        foreach (var material in matStore.GetMaterials())
        {
            int slotLength = matStore.GetMaterialUploadData(material!, slots, out var payload);
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