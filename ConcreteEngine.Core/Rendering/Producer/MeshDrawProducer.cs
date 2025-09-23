using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public struct MeshDrawEntity(MeshId meshId, MaterialId materialId, in Transform transform)
{
    public Transform Transform = transform;
    public MeshId MeshId = meshId;
    public MaterialId MaterialId = materialId;
}

public interface IMeshDrawSink : IDrawSink
{
    void Send(ReadOnlySpan<MeshDrawEntity> entities);
    void SendSingle(in MeshDrawEntity entity);
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
    public void SendSingle(in MeshDrawEntity entity)
    {
        EnsureCapacity(_idx);
        _entities[_idx++] = entity;
    }


    public void Initialize()
    {
    }

    public void BeginTick(in UpdateInfo update)
    {
        _idx = 0;
    }

    public void EndTick()
    {
    }

    public void EmitFrame(float alpha, in RenderGlobalSnapshot snapshot, RenderPipeline submitter)
    {
        if (_idx == 0) return;

        int counter = 0;
        for (int i = 0; i < _idx; i++)
        {
            ref var entity = ref _entities[i];

            _commands[counter] = new DrawCommand(
                meshId: entity.MeshId,
                drawCount: 0,
                materialId: entity.MaterialId
            );
            
            _transforms[counter] = new DrawTransformPayload(entity.Transform.GetTransform());

            _meta[counter] = new DrawCommandMeta(
                DrawCommandId.Mesh,
                RenderTargetId.Scene,
                DrawCommandQueue.Opaque,
                order: MetaOrders.OpaqueOrder(entity.MaterialId)
            );

            counter++;
            if (counter >= BatchSize)
            {
                submitter.SubmitDrawBatch(_commands, _meta, _transforms);
                counter = 0;
            }
        }

        if (counter > 0)
        {
            var commands = _commands.AsSpan(0, counter);
            var metas = _meta.AsSpan(0, counter);
            var transforms = _transforms.AsSpan(0, counter);
            submitter.SubmitDrawBatch(commands, metas, transforms);
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