using ConcreteEngine.Core.Rendering;
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
        spriteBatch.BeginBatch(0, SpriteFeature.SpriteTexture.ResourceId, SpriteFeature.SpriteShader.ResourceId);
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
        var cmd = spriteBatch.FlushBatch();

        submitter.SubmitDraw(cmd, new DrawCommandMeta(RenderTargetId.None, 0));
    }
}