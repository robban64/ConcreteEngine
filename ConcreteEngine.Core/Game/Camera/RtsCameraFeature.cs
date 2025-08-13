using Silk.NET.Input;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Game.Camera;

public sealed class RtsCameraFeature: GameFeature
{
    public override bool IsUpdateable => true;

    public override void Load()
    {
    }

    public override void Unload()
    {
    }

    public override void Update(float dt)
    {
        const float speed = 100;

        var input = Context.Input;
        var camera = Context.Graphics.Ctx.ViewTransform;
        if (input.IsKeyDown(Key.A))
        {
            camera.Position -= new Vector2D<float>(dt * speed, 0);
        }
        else if (input.IsKeyDown(Key.D))
        {
            camera.Position += new Vector2D<float>(dt * speed, 0);
        }
        

        if (input.IsKeyDown(Key.W)) camera.Position -= new Vector2D<float>(0, dt * speed);
        else if (input.IsKeyDown(Key.S)) camera.Position += new Vector2D<float>(0, dt * speed);

    }
    
}