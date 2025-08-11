using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Rendering.Sprite;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Module;

public sealed class SpriteModule : GameModule
{
    public SpriteBatchController SpriteBatch { get; set; }
    public Shader SpriteShader { get; set; } = null!;
    public Texture2D SpriteTexture { get; set; } = null!;
    public SpriteAtlas SpriteAtlas { get; set; } = null!;

    public Transform2D Transform { get; set; } = new()
    {
        Scale = new(100, 100),
    };

    public override bool IsUpdateable => true;
    public override bool IsRenderable => true;

    
    private float timer = 0;
    private int column = 0;
    private int row = 0;
    
    public SpriteModule(GameEngineContext context, int order) : base(context, order)
    {
    }

    public override void Load()
    {
        SpriteShader = Context.Assets.Get<Shader>("SpriteShader");
        SpriteTexture = Context.Assets.Get<Texture2D>("SpriteTexture");
        SpriteAtlas = new SpriteAtlas(9, 4);
        
        Context.Graphics.SpriteBatchController.CreateSpriteBatch("default", 128);

    }

    public override void Unload()
    {
    }


    public override void Update(float dt)
    {
        const float speed = 100;
        var input = Context.Input;
        if (input.IsKeyDown(Key.Left))
        {
            Transform.Position -= new Vector2D<float>(dt * speed, 0);
            row = 1;
        }
        else if (input.IsKeyDown(Key.Right))
        {
            Transform.Position += new Vector2D<float>(dt * speed, 0);
            row = 3;
        }
        

        if (input.IsKeyDown(Key.Up)) Transform.Position -= new Vector2D<float>(0, dt * speed);
        else if (input.IsKeyDown(Key.Down)) Transform.Position += new Vector2D<float>(0, dt * speed);
    }

    
    public override void Render(float dt)
    {
        timer += dt;
        if (timer > 0.125f)
        {
            timer = 0;
            column++;
            if (column >= 9) column = 0;
        }
        
        Context.Graphics.RenderPipeline.BindRenderPass(RenderTargetId.None);
        var spriteBatch = Context.Graphics.SpriteBatchController;
        spriteBatch.BeginBatch("default", SpriteTexture.Texture, SpriteShader.ShaderProgram);
        var cmd = SpriteBatchDrawItem.From(Transform, SpriteAtlas.GetOffset(column, row), SpriteAtlas.Scale);
        spriteBatch.SubmitSprite(cmd);
        spriteBatch.FlushBatch();
        

    }
}