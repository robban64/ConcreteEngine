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

public class SpriteFeature : IGameFeature, IDrawableFeature<SpriteDrawEntity>
{
    public int Order { get; set; }
    public bool IsUpdateable => true;

    public Shader SpriteShader { get; set; } = null!;
    public Texture2D SpriteTexture { get; set; } = null!;
    public SpriteAtlas SpriteAtlas { get; set; } = null!;

    private readonly List<SpriteDrawEntity> _spriteEntities = new(900);

    public void Load(GameFeatureContext context)
    {
        var assets = context.GetSystem<AssetSystem>();
        var renderer = context.GetSystem<RenderSystem>();

        SpriteShader = assets.Get<Shader>("SpriteShader");
        SpriteTexture = assets.Get<Texture2D>("SpriteTexture");
        SpriteAtlas = new SpriteAtlas(64, SpriteTexture.Width, SpriteTexture.Height);

        renderer.SpriteBatch.CreateSpriteBatch(0, 1024);

        for (int x = 0; x < 30; x++)
        {
            for (int y = 0; y < 30; y++)
            {
                _spriteEntities.Add(new SpriteDrawEntity
                {
                    Position = new Vector2(64 * x, 64 * y),
                    Scale = new Vector2(64, 64),
                    AtlasLocation = new Vector2D<int>(0, 3),
                    Direction = new Vector2D<int>(-1, 0),
                    Uv = SpriteAtlas.GetUvRect(0, 3)
                });
            }
        }
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


        var spanEntities = CollectionsMarshal.AsSpan(_spriteEntities);


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
                if (r == 0 && e.Position.X < 50) r = 1;
                else if (r == 1 && e.Position.X > 800) r = 0;
                else if(r ==  2 && e.Position.Y > 800) r = 3;
                else if(r ==  3 && e.Position.Y < 50) r = 2;

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

    public ReadOnlySpan<SpriteDrawEntity> GetDrawables()
    {
        return CollectionsMarshal.AsSpan(_spriteEntities);
    }
}