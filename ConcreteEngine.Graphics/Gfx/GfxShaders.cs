#region

using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxShaders
{
    private readonly GfxResourceDisposer _disposer;
    private readonly IDriverDebugger _drivDebug;

    private readonly ShaderStore _store;
    private readonly GlShaders _driver;

    internal GfxShaders(GfxContextInternal context)
    {
        _store = context.Resources.GfxStoreHub.ShaderStore;
        _driver = context.Driver.Shaders;
        _disposer = context.Disposer;
        _drivDebug = context.Driver.Debugger;
    }

    public ShaderId CreateShader(string vs, string fs, out int samplers)
    {
        var programRef = _driver.CreateShader(vs, fs);
        samplers = _driver.GetSamplersFromProgram(programRef);
        var meta = new ShaderMeta(samplers);
        return _store.Add(in meta, programRef);
    }

    public void RecreateShader(ShaderId shaderId, string vs, string fs, out int samplers)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(shaderId.Value, 0, nameof(shaderId));
        ArgumentException.ThrowIfNullOrEmpty(vs, nameof(shaderId));
        ArgumentException.ThrowIfNullOrEmpty(vs, nameof(shaderId));

        _drivDebug.ToggleDebug(false);
        GfxRefToken<ShaderId> oldRef = default, newRef = default;
        try
        {
            oldRef = _store.GetRefAndMeta(shaderId, out _);
            newRef = _driver.CreateShader(vs, fs);
        }
        finally
        {
            _drivDebug.ToggleDebug(true);
        }
        
        samplers = _driver.GetSamplersFromProgram(newRef);
        var meta = new ShaderMeta(samplers);
        _store.Replace(shaderId, in meta, newRef, out _);
        _disposer.EnqueueReplace(oldRef);
    }

    public List<(string, int)> GetUniformList(ShaderId shaderId)
    {
        var programRef = _store.GetRefHandle(shaderId);
        return _driver.GetUniformsFromProgram(programRef);
    }
}