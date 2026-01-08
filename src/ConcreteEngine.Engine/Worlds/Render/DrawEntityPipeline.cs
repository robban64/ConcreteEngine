using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Render.Processor;
using ConcreteEngine.Renderer.Draw;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class DrawEntityPipeline
{
    private const int DefaultCapacity = 512;
    private const int MaxCapacity = 1024 * 50;

    public static MaterialId BoundsMaterial;

    //...
    private int[] _byEntityId = [];
    private DrawEntity[] _drawEntities = [];
    //...

    private readonly RenderContext _renderCtx;

    private FrameEntityContext _frameEntityCtx = null!;
    private DrawCommandBuffer _commandBuffer = null!;

    public DrawEntityPipeline(RenderContext renderCtx)
    {
        _renderCtx = renderCtx;
        Array.Fill(_byEntityId, -1);
    }

    public void Attach(FrameEntityContext frameEntityCtx, DrawCommandBuffer commandBuffer)
    {
        _frameEntityCtx = frameEntityCtx;
        _commandBuffer = commandBuffer;
    }


    public void Begin()
    {
        EnsureCommandBuffer();
        EnsureCapacity(_frameEntityCtx.EcsCapacity);
        Array.Fill(_byEntityId, -1);
        _frameEntityCtx.BeginFrame();
    }


    public void ExecuteWorldObjects(World world)
    {
        WorldObjectProcessor.SubmitWorldObjects(_commandBuffer, world);
    }

    public void Execute()
    {
        if (_drawEntities.Length == 0) return;
        if (_drawEntities.Length != _byEntityId.Length)
            throw new IndexOutOfRangeException();

        // cull
        var len = CullEntities(_renderCtx.Camera.RenderView);
        _frameEntityCtx.VisibleCount = len;
        if (len == 0) return;

        // execute
        var ctx = new DrawEntityContext(len, _drawEntities, _byEntityId, _frameEntityCtx.GetRenderEntitySpan(),
            _frameEntityCtx.GetEntityWorldSpan());

        ExecuteCollectCommands(in ctx);
        ExecuteUploader(in ctx);

        AnimatorProcessor.Execute(_commandBuffer, _renderCtx.AnimationTable, new UnsafeSpan<int>(_byEntityId));
        ParticleProcessor.Execute(in ctx, _renderCtx.ParticleSystem);
    }

    private int CullEntities(in CameraRenderView renderView)
    {
        var ecsLen = _frameEntityCtx.RenderEcs.Count;
        var len = SpatialProcessor.CullEntities(_frameEntityCtx, _byEntityId, in renderView);

        if (len == 0) return 0;
        if ((uint)len > _drawEntities.Length || (uint)ecsLen > _byEntityId.Length)
            throw new IndexOutOfRangeException();

        return len;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteCollectCommands(in DrawEntityContext ctx)
    {
        RenderEntityCollector.CollectEntities(in ctx);
        DrawTagResolver.TagResolveEntities(in ctx);
        SpatialProcessor.TagDepthKeys(in ctx, _renderCtx.Camera);
        ParticleProcessor.TagParticles(in ctx, _renderCtx.ParticleSystem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteUploader(in DrawEntityContext ctx)
    {
        var uploader = _commandBuffer.GetDrawUploaderCtx();
        RenderEntityCollector.UploadDrawCommands(_renderCtx, in ctx, in uploader);
        SpatialProcessor.UploadTransform(_renderCtx, in ctx, in uploader);
        DrawTagResolver.UploadDebugBounds(_renderCtx, in ctx, in uploader, BoundsMaterial);
    }

    private void EnsureCommandBuffer()
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityLen = Ecs.Render.Core.Count + extraEntities;
        var animationLen = Ecs.Render.Stores<RenderAnimationComponent>.Store.Count + extraAnimations;

        EnsureCapacity(entityLen);
        _commandBuffer.EnsureBufferCapacity(entityLen);
        _commandBuffer.EnsureBoneBuffer(animationLen);
    }

    private void EnsureCapacity(int amount)
    {
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _drawEntities.Length);

        if (_drawEntities.Length >= amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_drawEntities.Length, amount);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        _drawEntities = new DrawEntity[newCap];
        //_entityIndices = new RenderEntityId[newCap];
        _byEntityId = new int[newCap];
        //_entityWorld = new Matrix4x4[newCap];

        Array.Fill(_byEntityId, -1);

        Logger.LogString(LogScope.World, $"Entity buffer resize {newCap}", LogLevel.Warn);
    }
}