using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Utils;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Game.Sprite;

public class SpriteFeature: IGameFeature
{
    public int Order { get; set; }
    public bool IsUpdateable => true;
    
    public Shader SpriteShader { get; set; } = null!;
    public Texture2D SpriteTexture { get; set; } = null!;
    public SpriteAtlas SpriteAtlas { get; set; } = null!;

    public List<SpriteEntity> SpriteEntities { get; set; } = new(256);

    public void Load(GameFeatureContext context)
    {
        var assets = context.GetSystem<AssetSystem>();
        var renderer = context.GetSystem<RenderSystem>();

        SpriteShader = assets.Get<Shader>("SpriteShader");
        SpriteTexture = assets.Get<Texture2D>("SpriteTexture");
        SpriteAtlas = new SpriteAtlas(9, 4);

        renderer.SpriteBatch.CreateSpriteBatch(0, 1024);

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                SpriteEntities.Add(new SpriteEntity
                {
                    Position = new Vector2D<float>(64 * x + 128, 64 * y + 128),
                    Scale = new Vector2D<float>(64, 64),
                });
            }
        }
    }
    
    private float timer = 0;
    private int column = 0;
    private int row = 0;

    private int direction = 1;

    public void UpdateTick(int tick)
    {
        const float speed = 6;

        foreach (var entity in SpriteEntities)
        {
            entity.Position.X += speed * direction;
        }
        
        
        timer += 1;
        if (timer >= 100)
        {
            direction = direction == 1 ? -1 : 1;
            timer -= 100;
            column = (column + 1) % 9;
        }
    }

    public void Unload()
    {
    }
}