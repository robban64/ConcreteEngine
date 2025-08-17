using System.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Input;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Game.Sprite;

public sealed class PlayerFeature : IGameFeature
{
    
    private float timer = 0;
    public int column = 0;
    public int row = 0;
    
    public bool IsUpdateable => true;
    public int Order { get; set; }

    public Shader SpriteShader { get; set; } = null!;
    public Texture2D SpriteTexture { get; set; } = null!;
    public SpriteAtlas SpriteAtlas { get; set; } = null!;

    public Transform2D Transform { get; set; } = new()
    {
        Scale = new(100, 100),
    };

    private InputSystem _input = null!;


    public void Load(GameFeatureContext context)
    {
        var assets = context.GetSystem<AssetSystem>();
        var renderer = context.GetSystem<RenderSystem>();
        _input = context.GetSystem<InputSystem>();

        SpriteShader = assets.Get<Shader>("SpriteShader");
        SpriteTexture = assets.Get<Texture2D>("SpriteTexture");
        SpriteAtlas = new SpriteAtlas(64, SpriteTexture.Width, SpriteTexture.Height);
        
        renderer.SpriteBatch.CreateSpriteBatch(0, 1024);

    }

    public void Unload()
    {
    }


    public void UpdateTick(int tick)
    {
        const float speed = 15;
        var input = _input;
        if (input.IsKeyDown(Key.Left))
        {
            Transform.Position -= new Vector2( speed, 0);
            row = 1;
        }
        else if (input.IsKeyDown(Key.Right))
        {
            Transform.Position += new Vector2( speed, 0);
            row = 3;
        }
        

        if (input.IsKeyDown(Key.Up)) Transform.Position -= new Vector2(0,  speed);
        else if (input.IsKeyDown(Key.Down)) Transform.Position += new Vector2(0,  speed);

        timer += 5;
        if (timer >= 30)
        {
            timer -= 30;
            column = (column + 1) % 9;
        }
            
/*
        timer += 0.33f;
        if (timer > 1f)
        {
            timer = 0;
            column++;
            if (column >= 9) column = 0;
        }
        */
    }

}