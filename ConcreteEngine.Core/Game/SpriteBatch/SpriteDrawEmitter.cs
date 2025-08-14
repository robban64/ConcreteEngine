using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Core.Rendering.SpriteBatching;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Game.SpriteBatch;

public sealed class SpriteDrawEmitter : IDrawCommandEmitter
{
    public int Order { get; set; }

    public SpriteFeature SpriteFeature { get; set; } = null!;

    public void Initialize(IFeatureRegistry registry)
    {
        SpriteFeature = registry.Get<SpriteFeature>();
    }

    private static readonly Matrix4X4<float> DefaultTransform =
        Transform2D.CreateTransformMatrix(Vector2D<float>.Zero, Vector2D<float>.One, 0);

    private Matrix4X4<float> _transformMatrix = Matrix4X4<float>.Identity;
    private ushort _textureId = 0;
    private ushort _shaderId = 0;

    public void Emit(DrawEmitterContext context,  DrawCommandSubmitter submitter)
    {
        var spriteBatch = context.SpriteBatch;
        spriteBatch.BeginBatch(0);
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                var pos = SpriteFeature.Transform.Position + new Vector2D<float>(x * 50, y * 50);

                var item = new SpriteDrawData(pos, SpriteFeature.Transform.Scale, 
                    SpriteFeature.SpriteAtlas.GetOffset(SpriteFeature.column, SpriteFeature.row), SpriteFeature.SpriteAtlas.Scale);
                spriteBatch.SubmitSprite(item);

            }
        }
        
        
        var result = spriteBatch.BuildBatch();
        var meta = new DrawCommandMeta(DrawCommandId.Sprite, RenderTargetId.None, 0);
        
        var cmd = new DrawCommandData(
            meshId: result.MeshId,
            materialId: MaterialId.Of(0), 
            drawCount: result.DrawCount,
            transform:  in DefaultTransform
        );
        submitter.SubmitDraw(in cmd, in meta);
    }
}