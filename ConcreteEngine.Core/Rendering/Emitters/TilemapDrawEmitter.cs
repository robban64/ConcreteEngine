#region

using System.Numerics;
using ConcreteEngine.Core.Game.Terrain;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Core.Rendering.Emitters;

public sealed class TilemapDrawEmitter : DrawCommandEmitter<TilemapStruct>
{
    protected override void EmitBatch(ReadOnlySpan<TilemapStruct> entities, in DrawEmitterContext ctx, DrawCommandSubmitter submitter, int order)
    {
        var transform = Transform2D.CreateTransformMatrix(Vector2.Zero, new Vector2(1, 1), 0);

        var tilemapBatcher = ctx.TilemapBatch;
        var result = tilemapBatcher.BuildBatch();
        var cmd = new DrawCommandData(
            meshId: result.GroundLayer.MeshId,
            drawCount: result.GroundLayer.DrawCount,
            materialId: MaterialId.Of(1),
            transform: in transform
        );

        var meta = new DrawCommandMeta(DrawCommandId.Tilemap, RenderTargetId.Scene, 0);
        submitter.SubmitDraw(cmd, in meta);
    }
}