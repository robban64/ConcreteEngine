#region

using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxShaders
{
    private readonly GfxStoreHub _resources;
    private readonly GlShaders _driver;

    internal GfxShaders(GfxContextInternal context)
    {
        _resources = context.Stores;
        _driver = context.Driver.Shaders;
    }

    public ShaderId CreateShader(string vs, string fs, out int samplers)
    {
        var programRef = _driver.CreateShader(vs, fs);
        samplers = _driver.GetSamplersFromProgram(programRef);
        var meta = new ShaderMeta(samplers);
        var shaderId = _resources.ShaderStore.Add(in meta, programRef);
        return shaderId;
    }

    public List<(string, int)> GetUniformList(ShaderId shaderId)
    {
        var programRef = _resources.ShaderStore.GetRefHandle(shaderId);
        return _driver.GetUniformsFromProgram(programRef);
    }
}