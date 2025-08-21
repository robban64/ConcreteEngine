#region

using System.Numerics;
using ConcreteEngine.Core.Game.Sprite;
using ConcreteEngine.Core.Rendering.Batchers.Sprite;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Core.Rendering.Emitters;

public sealed class SpriteDrawEmitter : DrawCommandEmitter<SpriteDrawEntity>
{
    private static readonly Matrix4x4 DefaultTransform =
        Transform2D.CreateTransformMatrix(Vector2.Zero, Vector2.One, 0);

    protected override void EmitBatch(ReadOnlySpan<SpriteDrawEntity> entities, in DrawEmitterContext ctx,
        DrawCommandSubmitter submitter, int order)
    {
        var alpha = ctx.Alpha;
        var spriteBatch = ctx.SpriteBatch;
        spriteBatch.BeginBatch(0);

        for (int i = 0; i < entities.Length; i++)
        {
            ref readonly var entity = ref entities[i];
            var pos = entity.Position;
            if (entity.PreviousPosition != default)
                pos = Vector2.Lerp(entity.PreviousPosition, entity.Position, alpha);
            var item = new SpriteDrawData(pos, entity.Scale, entity.Uv);
            spriteBatch.SubmitSprite(item);
        }


        var result = spriteBatch.BuildBatch();
        var meta = new DrawCommandMeta(DrawCommandId.Sprite, RenderTargetId.Scene, DrawCommandKind.Mesh, 0);

        var cmd = new DrawCommandMesh(
            meshId: result.MeshId,
            materialId: MaterialId.Of(1),
            drawCount: result.DrawCount,
            transform: in DefaultTransform
        );
        submitter.SubmitMeshDraw(in cmd, in meta);
    }
}