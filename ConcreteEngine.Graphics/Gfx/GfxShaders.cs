#region

using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxShaders
{
    private readonly GfxResourceDisposer _disposer;

    private readonly ShaderStore _store;
    private readonly GlShaders _driver;

    internal GfxShaders(GfxContextInternal context)
    {
        _store = context.Resources.GfxStoreHub.ShaderStore;
        _driver = context.Driver.Shaders;
        _disposer = context.Disposer;
    }

    public ShaderId CreateShader(string vs, string fs, out int samplers)
    {
        var programRef = _driver.CreateShader(vs, fs);
        samplers = _driver.GetSamplersFromProgram(programRef);
        var meta = new ShaderMeta(samplers);
        return _store.Add(in meta, programRef);
    }

    public ShaderId RecreateShader(ShaderId shaderId, string vs, string fs, out int samplers)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(shaderId.Value, 0, nameof(shaderId));
        var oldRef = _store.GetRefAndMeta(shaderId, out _);
        _disposer.EnqueueReplace(oldRef);

        var programRef = _driver.CreateShader(vs, fs);
        samplers = _driver.GetSamplersFromProgram(programRef);
        var meta = new ShaderMeta(samplers);
        return _store.Replace(shaderId, in meta, programRef, out _);
    }

    public List<(string, int)> GetUniformList(ShaderId shaderId)
    {
        var programRef = _store.GetRefHandle(shaderId);
        return _driver.GetUniformsFromProgram(programRef);
    }
}