#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Features.Sprite;
using ConcreteEngine.Core.Rendering.Batchers.Sprite;
using ConcreteEngine.Core.Rendering.Pipeline;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;

#endregion

namespace ConcreteEngine.Core.Rendering.Emitters;

public sealed class SpriteDrawEmitter : DrawCommandEmitter<SpriteFeatureDrawData>
{
    private static readonly Matrix4x4 DefaultTransform =
        Transform2D.CreateTransformMatrix(Vector2.Zero, Vector2.One, 0);

    protected override void EmitBatch(SpriteFeatureDrawData data, in DrawEmitterContext ctx,
        DrawCommandSubmitter submitter, int order)
    {
        if (data.Entities.Count == 0) return;

        var alpha = ctx.Alpha;
        var spriteBatch = ctx.SpriteBatch;
        var batches = data.Batches;
        var entities = CollectionsMarshal.AsSpan(data.Entities);

        int batchIdx = 0;
        foreach (var (materialId, start, size, layer) in batches)
        {
            var span = entities.Slice(start, size);
            spriteBatch.BeginBatch(batchIdx++);

            foreach (ref readonly var entity in span)
            {
                var pos = entity.Position;
                if (entity.PreviousPosition != default)
                    pos = Vector2.Lerp(entity.PreviousPosition, entity.Position, alpha);
                var item = new SpriteDrawData(pos, entity.Scale, entity.Uv);
                spriteBatch.SubmitSprite(item);
            }

            var result = spriteBatch.BuildBatch();
            var meta = new DrawCommandMeta(DrawCommandId.Sprite, DrawCommandTag.SpriteRenderer,
                RenderTargetId.Scene, layer);

            var cmd = new DrawCommandMesh(
                meshId: result.MeshId,
                materialId: materialId,
                drawCount: result.DrawCount,
                transform: in DefaultTransform
            );
            submitter.SubmitDraw(in cmd, in meta);
        }
    }
}