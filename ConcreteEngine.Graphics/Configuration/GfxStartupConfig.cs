using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics;

public interface IGfxStartupConfig<out T> where T : class
{
    T DriverContext { get; }
}

public sealed class GlStartupConfig(GL driverContext) : IGfxStartupConfig<GL>
{
    public GL DriverContext { get; } = driverContext;
}