#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Core.Utils;

#endregion

namespace ConcreteEngine.Core.Rendering;

public readonly record struct SpriteDrawBatch(MaterialId MaterialId, int Start, int End, byte Layer = 0);

public struct SpriteDrawEntity()
{
    public Vector2 Position = Vector2.Zero;
    public Vector2 PreviousPosition = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public UvRect Uv = default;
    public MaterialId MaterialId = default;
}

public sealed class SpriteDrawProducer : DrawCommandProducer<SpriteFeatureDrawData>
{
    private static readonly Matrix4x4 DefaultTransform =
        ModelTransform2D.CreateTransformMatrix(Vector2.Zero, Vector2.One, 0);
    
    public byte Layer { get; set; } = 1;
    
    protected override void EmitBatch(SpriteFeatureDrawData data, in CommandProducerContext ctx,
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
                var item = new SpriteBatchDrawItem(pos, entity.Scale, entity.Uv);
                spriteBatch.SubmitSprite(item);
            }

            var result = spriteBatch.BuildBatch();
            var meta = new DrawCommandMeta(DrawCommandId.Sprite, DrawCommandTag.SpriteRenderer,
                RenderTargetId.Scene, Layer);

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