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

public struct SpriteStruct
{
    public Vector2 Position = Vector2.Zero;
    public Vector2 PreviousPosition = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public float Rotation = 0;
    public Vector2D<int> AtlasLocation = Vector2D<int>.Zero;

    public SpriteStruct()
    {
    }
}

public class SpriteFeature : IGameFeature, IDrawableFeature<SpriteStruct>
{
    public int Order { get; set; }
    public bool IsUpdateable => true;

    public Shader SpriteShader { get; set; } = null!;
    public Texture2D SpriteTexture { get; set; } = null!;
    public SpriteAtlas SpriteAtlas { get; set; } = null!;

    private readonly List<SpriteStruct> _spriteEntities = new(16);

    public void Load(GameFeatureContext context)
    {
        var assets = context.GetSystem<AssetSystem>();
        var renderer = context.GetSystem<RenderSystem>();

        SpriteShader = assets.Get<Shader>("SpriteShader");
        SpriteTexture = assets.Get<Texture2D>("SpriteTexture");
        SpriteAtlas = new SpriteAtlas(64, SpriteTexture.Width, SpriteTexture.Height);

        renderer.SpriteBatch.CreateSpriteBatch(0, 1024);

        for (int x = 0; x < 20; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                _spriteEntities.Add(new SpriteStruct
                {
                    Position = new Vector2(64 * x, 64 * y),
                    Scale = new Vector2(64, 64),
                    AtlasLocation = new Vector2D<int>(0, 3),
                });
            }
        }
    }

    private float timer = 0;
    private float timer2 = 0;
    private int column = 0;
    private int row = 3;

    private int direction = 1;

    public void UpdateTick(int tick)
    {
        const float speed = 6;

        var spanEntities = CollectionsMarshal.AsSpan(_spriteEntities);

        foreach (ref var entity in spanEntities)
        {
            entity.PreviousPosition  = entity.Position;
            entity.Position.X += speed * direction;
        }

        timer2 += 1;
        timer += 1;
        if (timer2 > 2)
        {
            foreach (ref var entity in spanEntities)
            {
                entity.AtlasLocation.X = (entity.AtlasLocation.X + 1) % SpriteAtlas.Columns;
            }
            timer2 = 0;
        }
        if (timer >= 100)
        {
            direction = direction == 1 ? -1 : 1;
            timer -= 100;
            column = (column + 1) % 9;
            row += direction - 1;
            foreach (ref var entity in spanEntities)
            {
                entity.AtlasLocation.Y = entity.AtlasLocation.Y == 3 ? 1 : 3;
            }
        }
    }

    public void Unload()
    {
    }

    public ReadOnlySpan<SpriteStruct> GetDrawables()
    {
        return CollectionsMarshal.AsSpan(_spriteEntities);
    }
}