namespace ConcreteEngine.Graphics.Gfx;

internal interface IGfx<out T> : IGfx where T : class
{
    T Invoker { get; }
}

public interface IGfx;