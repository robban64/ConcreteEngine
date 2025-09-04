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
    
    private readonly DrawProduceArray<MeshDrawEntity> _entities = new(32);

    public void Send(ReadOnlySpan<MeshDrawEntity> payload)
    {
        _entities.EnsureCapacity(_idx + payload.Length);
        var entities = _entities.AsSpan();
        foreach (ref readonly var entity in payload)
        {
            entities[_idx++] = entity;
        }
    }

    public void SendSingle(in MeshDrawEntity payload)
    {
        _entities.EnsureCapacity(_idx);
        _entities.Data[_idx++] = payload;
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


    public void EmitFrame(float alpha, RenderPipeline submitter)
    {
        if(_idx == 0) return;
        var entities = _entities.AsSpan(0, _idx);
        foreach (ref var entity in entities)
        {
            var cmd = new DrawCommandMesh(
                meshId: entity.MeshId,
                drawCount: 0,
                materialId: entity.MaterialId,
                transform: entity.Transform.GetTransform()
            );

            var meta = new DrawCommandMeta(DrawCommandId.Mesh, DrawCommandTag.Mesh3D, RenderTargetId.Scene, 0);
            submitter.SubmitDraw(in cmd, in meta);

        }
    }
}