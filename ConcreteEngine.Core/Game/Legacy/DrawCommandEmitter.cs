using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Game.Legacy;

public interface IDrawCommandEmitter
{
    int Order { get; }
    void Emit(IGraphicsContext ctx, DrawCommandSubmitter submitter);
}

public class SpriteCommandEmitter : IDrawCommandEmitter
{
    public int Order { get; }
    public void Emit(IGraphicsContext ctx, DrawCommandSubmitter submitter)
    {
        //ctx.SpriteBatch()
    }
}
/*
public abstract class DrawCommandEmitter : IDrawCommandEmitter
{
    public void Emit<T>(IGraphicsContext ctx, T component, float dt) where T : GameComponent
    {
        throw new NotImplementedException();
    }
}
*/