using System.Numerics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
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
    private bool _hasUploadedMaterial;
    private bool _isInitialized;

    private readonly RenderEntityCore _ecs;

    private WorldBundle _worldBundle;

    private DrawCommandBuffer _commandBuffer = null!;

    private FrameEntityBuffer _frameBuffer = null!;
    private FrameProcessor _frameProcessor = null!;
    private RenderDispatcher _renderDispatcher = null!;

    private MaterialStore _materialStore = null!;

    private readonly RenderProgram _renderer;

    internal EngineRenderSystem(GraphicsRuntime graphics)
    {
        _ecs = Ecs.Render.Core;
        _renderer = new RenderProgram(graphics, PrimitiveMeshes.FsqQuad);
    }

    internal RenderProgram Program => _renderer;
    internal FrameEntityBuffer FrameEntityBuffer => _frameBuffer;

    internal int VisibleCount => _frameBuffer.VisibleCount;
    internal ReadOnlySpan<RenderEntityId> VisibleEntities => _frameBuffer.VisibleEntities;
    internal ReadOnlySpan<Matrix4x4> EntityWorldSpan => _frameBuffer.WorldMatrices;

    internal void Initialize(MaterialStore materialStore, World world)
    {
        _worldBundle = world.Bundle;

        _materialStore = materialStore;
        _commandBuffer = _renderer.CommandBuffer;
        _frameBuffer = new FrameEntityBuffer();

        _frameProcessor = new FrameProcessor();
        _renderDispatcher = new RenderDispatcher(_ecs, _worldBundle, _frameBuffer, _commandBuffer);

        _isInitialized = true;
    }

    internal void Render(in RenderFrameArgs args)
    {
        _renderer.PrepareFrame(in args);
        _worldBundle.Camera.WriteSnapshot(args.Alpha, _renderer.RenderCamera);

        SubmitMaterialData();
        EnsureCommandBuffer();

        _frameBuffer.Prepare();

        _frameProcessor.Execute(args.DeltaTime, args.Alpha);
        _renderDispatcher.Execute();

        // prepare buffers
        _renderer.CollectDrawBuffers();

        // upload buffers to gpu
        _renderer.UploadFrameData();

        _renderer.Render();
    }

    private void SubmitMaterialData()
    {
        var matStore = _materialStore;
        if (!matStore.HasDirtyMaterials && _hasUploadedMaterial) return;
        if (matStore.HasDirtyMaterials) _hasUploadedMaterial = false;

        matStore.ClearDirtyMaterials();

        Span<TextureSlotInfo> slots = stackalloc TextureSlotInfo[RenderLimits.TextureSlots];
        foreach (var material in matStore.MaterialSpan)
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