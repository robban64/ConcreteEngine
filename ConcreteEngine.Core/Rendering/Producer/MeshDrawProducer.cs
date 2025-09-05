using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public struct MeshDrawEntity(MeshId meshId, MaterialId materialId, in Transform transform)
{
    public MeshId MeshId = meshId;
    public MaterialId MaterialId = materialId;
    public Transform Transform = transform;
}

public interface IMeshDrawSink : IDrawSink
{
    void Send(ReadOnlySpan<MeshDrawEntity> payload);
    void SendSingle(in MeshDrawEntity payload);
}

public sealed class MeshDrawProducer : IDrawCommandProducer, IMeshDrawSink
{
    private const int BatchSize = 32;

    private CommandProducerContext _context = null!;

    private int _idx = 0;

    private MeshDrawEntity[] _entities = new MeshDrawEntity[32];

    private readonly DrawCommandMesh[] _commands = new DrawCommandMesh[BatchSize];
    private readonly DrawCommandMeta[] _meta = new DrawCommandMeta[BatchSize];


    public void Send(ReadOnlySpan<MeshDrawEntity> payload)
    {
        EnsureCapacity(_idx + payload.Length);
        payload.CopyTo(_entities.AsSpan(_idx));
        _idx += payload.Length;
    }

    public void SendSingle(in MeshDrawEntity payload)
    {
        EnsureCapacity(_idx);
        _entities[_idx++] = payload;
    }


    public void Initialize()
    {
    }

    public void BeginTick(in UpdateMetaInfo updateMeta)
    {
        _idx = 0;
    }

    public void EndTick()
    {
    }

    public void EmitFrame(float alpha, IRenderPipeline submitter)
    {
        if (_idx == 0) return;

        int counter = 0;
        for (int i = 0; i < _idx; i++)
        {
            ref var entity = ref _entities[i];

            _commands[counter] = new DrawCommandMesh(
                meshId: entity.MeshId,
                drawCount: 0,
                materialId: entity.MaterialId,
                transform: entity.Transform.GetTransform()
            );

            _meta[counter] = new DrawCommandMeta(
                DrawCommandId.Mesh,
                RenderTargetId.Scene,
                DrawCommandQueue.Opaque,
                order: MetaOrders.OpaqueOrder(entity.MaterialId)
            );

            counter++;
            if (counter >= BatchSize)
            {
                submitter.SubmitDrawBatch<DrawCommandMesh>(_commands, _meta);
                counter = 0;
            }
        }

        if (counter > 0)
        {
            submitter.SubmitDrawBatch<DrawCommandMesh>(
                _commands.AsSpan(0, counter), _meta.AsSpan(0, counter));
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