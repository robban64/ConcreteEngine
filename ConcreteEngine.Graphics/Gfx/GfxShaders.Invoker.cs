using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxShaderInvoker
{
    private readonly IGraphicsDriver _driver;

    internal GfxShaderInvoker(GfxContext context)
    {
        _driver = context.Driver;
    }
    
    public ResourceRefToken<ShaderId> CreateShader(string vertexSource, string fragmentSource, out int samples, out List<(string, int)> uniforms)
    {
        var programRef = _driver.Shaders.CreateShader(vertexSource, fragmentSource);
        _driver.Shaders.GetUniformsFromProgram(in programRef.Handle, out  uniforms, out  samples);
        return programRef;
    }
}