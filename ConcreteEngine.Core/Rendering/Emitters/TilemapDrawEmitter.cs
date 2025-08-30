#region

using System.Numerics;
using ConcreteEngine.Core.Features.Terrain;
using ConcreteEngine.Core.Rendering.Pipeline;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;

#endregion

namespace ConcreteEngine.Core.Rendering.Emitters;

public sealed class TilemapDrawEmitter : DrawCommandEmitter<TilemapDrawData>
{
    protected override void EmitBatch(TilemapDrawData data, in DrawEmitterContext ctx,
        DrawCommandSubmitter submitter, int order)
    {
        if(data.Count == 0) return;
        
        var transform = Transform2D.CreateTransformMatrix(Vector2.Zero, new Vector2(1, 1), 0);

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