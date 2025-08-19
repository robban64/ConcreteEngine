#region

using System.Numerics;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Core.Rendering.Sprite;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Core.Game.Sprite;

public sealed class SpriteDrawEmitter : IDrawCommandEmitter
{
    private static readonly Matrix4x4 DefaultTransform =
        Transform2D.CreateTransformMatrix(Vector2.Zero, Vector2.One, 0);

    public int Order { get; set; }

    public SpriteFeature SpriteFeature { get; set; } = null!;

    public void Initialize(IFeatureRegistry registry)
    {
        SpriteFeature = registry.Get<SpriteFeature>();
    }

    public void Emit(DrawEmitterContext context, DrawCommandSubmitter submitter)
    {
        var atlas = SpriteFeature.SpriteAtlas;
        var spriteBatch = context.SpriteBatch;
        spriteBatch.BeginBatch(0);

        var entities = SpriteFeature.GetDrawables();
        
        
        for (int i = 0; i < entities.Length; i++)
        {
            ref readonly var entity = ref entities[i];
            var pos = entity.Position;
            if (entity.PreviousPosition != default)
                pos = Vector2.Lerp(entity.PreviousPosition, entity.Position, context.Alpha);
            var uv = atlas.GetUvRect(entity.AtlasLocation.X, entity.AtlasLocation.Y);
            var item = new SpriteDrawData(pos, entity.Scale, uv);
            spriteBatch.SubmitSprite(item);
        }


        var result = spriteBatch.BuildBatch();
        var meta = new DrawCommandMeta(DrawCommandId.Sprite, RenderTargetId.Scene, 0);

        var cmd = new DrawCommandData(
            meshId: result.MeshId,
            materialId: MaterialId.Of(0),
            drawCount: result.DrawCount,
            transform: in DefaultTransform
        );
        submitter.SubmitDraw(in cmd, in meta);

    }
}