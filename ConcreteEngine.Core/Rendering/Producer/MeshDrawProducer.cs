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
    private CommandProducerContext _context = null!;

    private int _idx = 0;

    private MeshDrawEntity[] _entities = new MeshDrawEntity[32];

    public void Send(ReadOnlySpan<MeshDrawEntity> payload)
    {
        EnsureCapacity(_idx + payload.Length);
        var entities = _entities.AsSpan();
        foreach (ref readonly var entity in payload)
        {
            entities[_idx++] = entity;
        }
    }
    
        
    public void SendSingle(in MeshDrawEntity payload)
    {
        EnsureCapacity(_idx);
        _entities[_idx++] = payload;
    }
    
    public void AttachContext(CommandProducerContext ctx)
    {
        _context = ctx;
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
        var entities = _entities.AsSpan(0, _idx);
        foreach (ref var entity in entities)
        {
            var cmd = new DrawCommandMesh(
                meshId: entity.MeshId,
                drawCount: 0,
                materialId: entity.MaterialId,
                transform: entity.Transform.GetTransform()
            );

            var meta = new DrawCommandMeta(DrawCommandId.Mesh,  RenderTargetId.Scene,
                DrawCommandQueue.Opaque, order: MetaOrders.OpaqueOrder(entity.MaterialId));
            
            submitter.SubmitDraw(in cmd, in meta);
        }
    }
    
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