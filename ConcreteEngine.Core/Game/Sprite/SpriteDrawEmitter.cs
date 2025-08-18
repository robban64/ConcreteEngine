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

    private List<Vector2> _previousPositions = new(64);
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

        for (int i = 0; i < SpriteFeature.SpriteEntities.Count; i++)
        {
            var entity = SpriteFeature.SpriteEntities[i];
            var pos = entity.Position;
            if (_previousPositions.Count > 0 && (i == 0 || i == 1))
                pos = Vector2.Lerp(entity.Position, _previousPositions[i], context.Alpha);
            var uv = atlas.GetUvRect(entity.AtlasLocation.X, entity.AtlasLocation.Y);
            var item = new SpriteDrawData(pos, entity.Scale, uv);
            spriteBatch.SubmitSprite(item);
        }

        /*
        var pos = PlayerFeature.Transform.Position;
        var item = new SpriteDrawData(pos, PlayerFeature.Transform.Scale,
            PlayerFeature.SpriteAtlas.GetOffset(PlayerFeature.column, PlayerFeature.row), PlayerFeature.SpriteAtlas.Scale);
        spriteBatch.SubmitSprite(item);
        */

        var result = spriteBatch.BuildBatch();
        var meta = new DrawCommandMeta(DrawCommandId.Sprite, RenderTargetId.Scene, 0);

        var cmd = new DrawCommandData(
            meshId: result.MeshId,
            materialId: MaterialId.Of(0),
            drawCount: result.DrawCount,
            transform: in DefaultTransform
        );
        submitter.SubmitDraw(in cmd, in meta);

        _previousPositions.Clear();
        foreach (var t in SpriteFeature.SpriteEntities)
        {
            _previousPositions.Add(new Vector2(t.Position.X, t.Position.Y));
        }
    }
}