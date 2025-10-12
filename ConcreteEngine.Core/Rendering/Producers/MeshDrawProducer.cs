#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Producers;

public struct MeshDrawEntity(MeshId meshId, MaterialId materialId, ref Transform transform)
{
    public Transform Transform = transform;
    public MeshId MeshId = meshId;
    public MaterialId MaterialId = materialId;
}

public interface IMeshDrawSink : IDrawSink
{
    void Send(ReadOnlySpan<MeshDrawEntity> entities);
    void SendSingle(MeshDrawEntity entity);
}

public sealed class MeshDrawProducer : IDrawCommandProducer, IMeshDrawSink
{
    private const int BatchSize = 32;

    private CommandProducerContext _context = null!;

    private int _idx = 0;

    private MeshDrawEntity[] _entities = new MeshDrawEntity[32];

    private DrawTransformPayload[] _transforms = new DrawTransformPayload[BatchSize];
    private readonly DrawCommand[] _commands = new DrawCommand[BatchSize];
    private readonly DrawCommandMeta[] _meta = new DrawCommandMeta[BatchSize];


    public void Send(ReadOnlySpan<MeshDrawEntity> entities)
    {
        EnsureCapacity(_idx + entities.Length);
        entities.CopyTo(_entities.AsSpan(_idx));
        _idx += entities.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendSingle(MeshDrawEntity entity)
    {
        EnsureCapacity(_idx);
        _entities[_idx++] = entity;
    }


    public void Initialize()
    {
    }

    public void BeginTick(in UpdateTickInfo tick)
    {
        _idx = 0;
    }

    public void EndTick()
    {
    }

    public void EmitFrame(float alpha, in RenderSceneState snapshot, DrawCommandBuffer submitter)
    {
        if (_idx == 0) return;

        int counter = 0;
        for (int i = 0; i < _idx; i++)
        {
            ref var entity = ref _entities[i];
            ref var transform = ref entity.Transform;

            _commands[counter] = new DrawCommand(
                meshId: entity.MeshId,
                drawCount: 0,
                materialId: entity.MaterialId
            );

            TransformUtils.CreateModelMatrix(
                transform.Position,
                transform.Scale,
                transform.Rotation,
                out var modelMat
            );
            _transforms[counter] = new DrawTransformPayload(in modelMat);

            _meta[counter] = new DrawCommandMeta(
                DrawCommandId.Mesh,
                DrawCommandQueue.Opaque,
                PassMask.Default
                //MetaOrders.OpaqueOrder(entity.MaterialId)
            );

            counter++;
            if (counter >= BatchSize)
            {
                submitter.SubmitDrawBatch(new DrawCommandData(_commands, _meta, _transforms));
                counter = 0;
            }
        }

        if (counter > 0)
        {
            var commands = _commands.AsSpan(0, counter);
            var metas = _meta.AsSpan(0, counter);
            var transforms = _transforms.AsSpan(0, counter);
            submitter.SubmitDrawBatch(new DrawCommandData(commands, metas, transforms));
        }
    }

    public void AttachContext(CommandProducerContext ctx) => _context = ctx;


    private void EnsureCapacity(int size)
    {
        if (_entities.Length < size + 1)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(size, 50_000);
            var newSize = int.Max(_entities.Length * 2, size);
            Array.Resize(ref _entities, newSize);
        }
    }
}