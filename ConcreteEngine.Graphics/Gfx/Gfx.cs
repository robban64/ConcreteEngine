namespace ConcreteEngine.Graphics.Gfx;

internal interface IGfxInvoker;

internal interface IGfx<out T> : IGfx where T : class, IGfxInvoker
{
    T Invoker { get; }
}

public interface IGfx;