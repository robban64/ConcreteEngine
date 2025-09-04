using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;


public struct MeshDrawEntity(MeshId meshId, MaterialId materialId, in Transform transform)
{
    public MeshId MeshId = meshId;
    public MaterialId MaterialId = materialId;
    public Transform Transform = transform;
}

public sealed class MeshDrawData
{
    public int Count { get; set; }
    public MeshDrawEntity[] Entities { get; set; } = null!;
}

public sealed class MeshDrawProducer : DrawCommandProducer<MeshDrawData>
{
    public override void OnInitialize()
    {

    }

    protected override void EmitCommands(float alpha, MeshDrawData data, DrawCommandSubmitter submitter)
    {
        if(data.Entities == null || data.Entities.Length == 0 || data.Count == 0) return;
        
        var entities = data.Entities.AsSpan(0,data.Count);
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