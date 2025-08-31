#region

using System.Numerics;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Transforms;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class TilemapDrawProducer : DrawCommandProducer<TilemapDrawData>
{
    protected override void EmitCommands(TilemapDrawData data,  CommandProducerContext ctx,
        DrawCommandSubmitter submitter, int order)
    {
        if(data.Count == 0) return;
        
        var transform = ModelTransform2D.CreateTransformMatrix(Vector2.Zero, new Vector2(1, 1), 0);

        var tilemapBatcher = ctx.TilemapBatch;
        var result = tilemapBatcher.BuildBatch();
        
        var cmd = new DrawCommandMesh(
            meshId: result.GroundLayer.MeshId,
            drawCount: result.GroundLayer.DrawCount,
            materialId: data.MaterialId,
            transform: in transform
        );

        var meta = new DrawCommandMeta(DrawCommandId.Tilemap, DrawCommandTag.SpriteRenderer, RenderTargetId.Scene, 0);
        submitter.SubmitDraw(in cmd, in meta);
    }
}