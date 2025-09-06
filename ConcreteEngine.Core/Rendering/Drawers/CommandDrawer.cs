#region

using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;

internal interface ICommandDrawer
{
    void AttachContext(CommandDrawerContext context);
    void Prepare(in RenderGlobalSnapshot renderGlobals);
}

internal abstract class CommandDrawer<T> : ICommandDrawer where T : unmanaged, IDrawCommand
{
    public CommandDrawerContext Context {get; private set;} = null!;
    protected RenderGlobalSnapshot RenderGlobals;

    protected IGraphicsContext Gfx = null!;



    public abstract void Draw(in T cmd);
    protected virtual void Initialize()
    {
    }
    
    public void Prepare(in RenderGlobalSnapshot renderGlobals)
    {
        RenderGlobals = renderGlobals;
    }
    
    public void AttachContext(CommandDrawerContext context)
    {
        Context = context;
        Gfx = context.Graphics.Gfx;
    }


}
