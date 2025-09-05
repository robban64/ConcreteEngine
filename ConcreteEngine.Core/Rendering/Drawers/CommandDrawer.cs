#region

using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;

internal interface ICommandDrawer
{
    public void AttachContext(CommandDrawerContext context);
}

internal abstract class CommandDrawer<T> : ICommandDrawer where T : unmanaged, IDrawCommand
{
    public CommandDrawerContext  Context {get; private set;} = null!;

    protected IGraphicsContext Gfx = null!;

    public abstract void Draw(in T cmd);
    protected virtual void Initialize()
    {
    }
    
    public void AttachContext(CommandDrawerContext context)
    {
        Context = context;
        Gfx = context.Graphics.Gfx;
    }


}
