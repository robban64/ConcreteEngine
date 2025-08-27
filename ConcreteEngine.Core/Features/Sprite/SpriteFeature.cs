#region

using System.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Data;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Features.Sprite;




public class SpriteFeature : GameFeature, IDrawableFeature<SpriteFeatureDrawData>
{
    public override bool IsUpdateable => true;
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    public Shader SpriteShader { get; set; } = null!;
    public Texture2D SpriteTexture { get; set; } = null!;
    public SpriteAtlas SpriteAtlas { get; set; } = null!;

    private readonly List<(int, int)> _batches = [];
    private SpriteDrawEntity[] _entities = null!;

    private readonly SpriteFeatureDrawData _drawData = new();
    
    private int _animationCountdown = 3;
    private int _dirCountdown = 20;
    private int _currentFrame = 0;

    public override void Initialize()
    {
        SpriteShader = Context.AssetSystem.Get<Shader>("SpriteShader");
        SpriteTexture = Context.AssetSystem.Get<Texture2D>("SpriteTexture");
        SpriteAtlas = new SpriteAtlas(64, SpriteTexture.Width, SpriteTexture.Height);
        
        Context.RenderSystem.SpriteBatch.CreateSpriteBatch(0, 1024);
        Context.RenderSystem.SpriteBatch.CreateSpriteBatch(1, 1024);
        Context.RenderSystem.SpriteBatch.CreateSpriteBatch(2, 1024);
        Context.RenderSystem.SpriteBatch.CreateSpriteBatch(3, 1024);

        _entities = new SpriteDrawEntity[900*4];

        for (int i = 0; i < 4; i++)
        {
            _batches.Add((i*900, 900));
            for (int j = 0; j < 900; j++)
            {
                var dir = new Vector2(i == 0 || i == 2 ? 1 : 0, i == 1 || i == 3 ? 1 : 0);
                CreateBatch(_entities, i*900, dir);
            }
        }
    }

    public override void UpdateTick(int tick)
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

        UpdateEntities(doRandomize, speed);
    }

    public override void Update(in FrameMetaInfo frameCtx)
    {
    }
    
    public SpriteFeatureDrawData GetDrawables()
    {
        _drawData.Batches = _batches;
        _drawData.Entities = _entities;
        return _drawData;
    }

    private void CreateBatch(SpriteDrawEntity[] batch, int start, Vector2 offsetPosition)
    {
        int i = 0;
        for (int x = 0; x < 30; x++)
        {
            for (int y = 0; y < 30; y++)
            {
                batch[i+start] = (new SpriteDrawEntity
                {
                    Position = new Vector2(64 * x, 64 * y) + offsetPosition * 64 * 4,
                    Scale = new Vector2(64, 64),
                    AtlasLocation = new Vector2D<int>(0, 3),
                    Direction = new Vector2(-1, 0),
                    Uv = SpriteAtlas.GetUvRect(0, 3)
                });
                i++;
            }
        }
    }

    private void UpdateEntities(bool doRandomize, float speed)
    {
        var spanEntities = _entities.AsSpan();

        for (int i = 0; i < spanEntities.Length; i++)
        {
            ref var entity = ref spanEntities[i];
            entity.PreviousPosition = entity.Position;
            entity.Position.X += speed * entity.Direction.X;
            entity.Position.Y += speed * entity.Direction.Y;
            var d = (int)MathF.Ceiling((MathF.Abs(entity.Direction.X) + MathF.Abs(entity.Direction.Y)) / 2f);
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
                else if (r == 2 && e.Position.Y >= 2048) r = 3;
                else if (r == 3 && e.Position.Y < 10) r = 2;

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


}