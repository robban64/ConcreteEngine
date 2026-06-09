using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher : IDisposable
{
    public int VisibleCount { get; private set; }

    private readonly Camera _camera;
    private readonly CameraFrustum _frustum;
    private readonly DrawCommandBuffer _commandBuffer;
    private readonly RenderUploadBuffers _uploadBuffers;
    private readonly AnimatorProcessor _animatorProcessor;

    internal RenderDispatcher(CameraManager cameraManager,AnimationSystem animations, RenderUploadBuffers uploadBuffers)
    {
        ArgumentNullException.ThrowIfNull(cameraManager);
        ArgumentNullException.ThrowIfNull(animations);
        ArgumentNullException.ThrowIfNull(uploadBuffers);
        if (cameraManager.Camera == null! || cameraManager.Frustum == null!)
            throw new ArgumentNullException(nameof(cameraManager));
        
        _camera = cameraManager.Camera;
        _frustum = cameraManager.Frustum;
        _uploadBuffers = uploadBuffers;
        _commandBuffer = uploadBuffers.Commands;
        _animatorProcessor = new AnimatorProcessor(animations, uploadBuffers.Skinning);
    }


    public void Prepare(TerrainSystem terrain)
    {
        EnsureCommandBuffer();
        terrain.SubmitDrawTerrain(_commandBuffer, _frustum);

        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
        var cmd = new DrawCommand(Skybox.Current.MeshId, Skybox.Current.MaterialId);
        _commandBuffer.Submit(cmd, meta, in DrawCommandBuffer.TransformIdentity);

    }

    internal void Execute()
    {
        VisibleCount = CullEntities();

        if (VisibleCount == 0) return;
        _animatorProcessor.Execute();
        
        CollectEntities();
        UploadDrawCommands();
        //DrawTagProcessor.UploadDebugBounds(submitOffset, visibleByIndices, _commandBuffer, _uploadBuffers.Effects);
    }

    private unsafe int CullEntities()
    {
        var visibleCount = 0;
        var length = Ecs.Render.Core.Count;
        var bounds = Ecs.Render.Core.GetBoundsPtr();
        var coreEntities = Ecs.Render.Core.GetCoreEntityPtr();
        for(var i = 0; i < length; i++)
        {
            if(!coreEntities[i].Alive) continue;
            var visible = _frustum.IntersectsBox(in bounds[i]);
            visible &= coreEntities[i].ToggleVisibility(VisibilityFlags.Culled, visible) == 0;
            if (visible) visibleCount++;
            
        }
        return visibleCount;
    }

    private unsafe void CollectEntities()
    {
        var index = 0;
        var sources = Ecs.Render.Core.GetSourcePtr();
        var transforms = Ecs.Render.Core.GetTransformPtr();
        var ctx = _commandBuffer.GetCommandMetaSpan();
        foreach (var entity in Ecs.Render.Core.VisibilityQuery())
        {
            ref readonly var source = ref sources[entity.Index()];
            ref readonly var translation = ref transforms[entity.Index()].Translation;
            var depth = _camera.MakeDepthKey(translation);
            depth = source.Queue < DrawCommandQueue.Transparent ? depth : (ushort)(ushort.MaxValue - depth);
            
            ctx.At1(index) =
                new DrawCommand(source.Mesh, source.Material, animationSlot: source.AnimationSlot);
            
            ctx.At2(index) =
                new DrawCommandMeta(DrawCommandId.Model, source.Queue, source.Mask, depth);

            ++index;
        }
    }

    private unsafe void UploadDrawCommands()
    {
        var parentMatrices = Ecs.Render.Core.GetMatrixPtr();
        foreach (var entity in Ecs.Render.Core.VisibilityQuery())
        {
            ref readonly var world = ref parentMatrices[entity.Index()];
            ref var bufferData = ref _commandBuffer.SubmitDraw();
            bufferData.Model = world;
            MatrixMath.CreateNormalMatrix(ref bufferData.Normal, in world);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCommandBuffer()
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityLen = Ecs.Render.Core.Count + extraEntities;
        var animationLen = Ecs.Render.Stores<SkinningComponent>.Store.Count + extraAnimations;

        _uploadBuffers.Commands.EnsureCapacity(entityLen);
        _uploadBuffers.Skinning.EnsureCapacity(animationLen);
    }

    public void Dispose() => _animatorProcessor.Dispose();
}