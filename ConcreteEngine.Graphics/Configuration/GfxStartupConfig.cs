using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics;

public interface IGfxStartupConfig<out T> where T : class
{
    T DriverContext { get; }
}
public sealed record GlStartupConfig(GL DriverContext) : IGfxStartupConfig<GL>;
