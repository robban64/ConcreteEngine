#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Utils;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Game.Sprite;


public struct SpriteDrawEntityBatch(List<SpriteDrawEntity> batch)
{
    public readonly List<SpriteDrawEntity> Batches = batch;
}
public struct SpriteDrawEntity
{
    public Vector2 Position = Vector2.Zero;
    public Vector2 PreviousPosition = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public Vector2D<int> AtlasLocation = Vector2D<int>.Zero;
    public Vector2D<int> Direction = Vector2D<int>.Zero;
    public UvRect Uv = default;
    public SpriteDrawEntity()
    {
    }
}

public class SpriteFeature : IDrawableFeature<SpriteDrawEntityBatch>
{
    public int Order { get; set; }
    public bool IsUpdateable => true;
    public bool IsDrawable => true;
    public int DrawOrder => 0;

    public Shader SpriteShader { get; set; } = null!;
    public Texture2D SpriteTexture { get; set; } = null!;
    public SpriteAtlas SpriteAtlas { get; set; } = null!;

    private readonly List<SpriteDrawEntity> _batch1 = new(900);
    private readonly List<SpriteDrawEntity> _batch2 = new(900);
    private readonly List<SpriteDrawEntity> _batch3 = new(900);
    private readonly List<SpriteDrawEntity> _batch4 = new(900);

    public void Load(GameFeatureContext context, int order)
    {
        Order = order;
        var assets = context.GetSystem<AssetSystem>();
        var renderer = context.GetSystem<RenderSystem>();

        SpriteShader = assets.Get<Shader>("SpriteShader");
        SpriteTexture = assets.Get<Texture2D>("SpriteTexture");
        SpriteAtlas = new SpriteAtlas(64, SpriteTexture.Width, SpriteTexture.Height);
        renderer.SpriteBatch.CreateSpriteBatch(0, 1024);
        renderer.SpriteBatch.CreateSpriteBatch(1, 1024);
        renderer.SpriteBatch.CreateSpriteBatch(2, 1024);
        renderer.SpriteBatch.CreateSpriteBatch(3, 1024);

        CreateBatch(_batch1, new Vector2(0,0));
        CreateBatch(_batch2, new Vector2(0,1));
        CreateBatch(_batch3, new Vector2(1,0));
        CreateBatch(_batch4, new Vector2(1,1));

    }

    int _animationCountdown = 3;
    int _dirCountdown = 20;
    private int _currentFrame = 0;

    public void UpdateTick(int tick)
    {
        const float speed = 2;

        _animationCountdown--;
        _dirCountdown--;
        bool doAnimate = _animationCountdown == 0;
        bool doRandomize = _dirCountdown == 0;
        if (doAnimate)
        {
            if (++_currentFrame % 9 == 0) _currentFrame = 0;
            _animationCountdown = 3;
        }

        UpdateEntities(_batch1, doRandomize, speed);
        UpdateEntities(_batch2, doRandomize, speed);
        UpdateEntities(_batch3, doRandomize, speed);
        UpdateEntities(_batch4, doRandomize, speed);

    }

    private void CreateBatch(List<SpriteDrawEntity> batch, Vector2 offsetPosition)
    {
        for (int x = 0; x < 30; x++)
        {
            for (int y = 0; y < 30; y++)
            {
                batch.Add(new SpriteDrawEntity
                {
                    Position = new Vector2(64 * x, 64 * y) + (offsetPosition * 64 * 4),
                    Scale = new Vector2(64, 64),
                    AtlasLocation = new Vector2D<int>(0, 3),
                    Direction = new Vector2D<int>(-1, 0),
                    Uv = SpriteAtlas.GetUvRect(0, 3)
                });
            }
        }
    }

    private void UpdateEntities(List<SpriteDrawEntity> list,bool doRandomize, float speed)
    {
        
        var spanEntities = CollectionsMarshal.AsSpan(list);


        for (int i = 0; i < spanEntities.Length; i++)
        {
            ref var entity = ref spanEntities[i];
            entity.PreviousPosition = entity.Position;
            entity.Position.X += speed * entity.Direction.X;
            entity.Position.Y += speed * entity.Direction.Y;
            var d = (int)MathF.Ceiling((Math.Abs(entity.Direction.X) + Math.Abs(entity.Direction.Y)) / 2f);
            entity.AtlasLocation.X = _currentFrame * d;
            entity.Uv = SpriteAtlas.GetUvRect(entity.AtlasLocation.X, entity.AtlasLocation.Y);
        }

        if (doRandomize)
        {
            for (int i = 0; i < spanEntities.Length; i++)
            {
                ref var e = ref spanEntities[i];
                e.Direction.X = 0;
                e.Direction.Y = 0;

                var r = Random.Shared.Next(0, 5);
                if (r == 0 && e.Position.X < 10) r = 1;
                else if (r == 1 && e.Position.X >= 2048) r = 0;
                else if(r ==  2 && e.Position.Y >= 2048) r = 3;
                else if(r ==  3 && e.Position.Y < 10) r = 2;

                switch (r)
                {
                    case 0:
                        e.Direction.X = -1;
                        e.AtlasLocation.Y = 1;
                        break;
                    case 1:
                        e.Direction.X = 1;
                        e.AtlasLocation.Y = 3;
                        break;
                    case 2:
                        e.Direction.Y = 1;
                        e.AtlasLocation.Y = 2;
                        break;
                    case 3:
                        e.Direction.Y = -1;
                        e.AtlasLocation.Y = 0;
                        break;
                }
            }

            _dirCountdown = 20;
        }
    }

    public void Unload()
    {
    }

    public ReadOnlySpan<SpriteDrawEntityBatch> GetDrawables()
    {
        List<SpriteDrawEntityBatch> batch = [new (_batch1),new(_batch2), new (_batch3), new (_batch4)];
        return CollectionsMarshal.AsSpan(batch);
    }


}