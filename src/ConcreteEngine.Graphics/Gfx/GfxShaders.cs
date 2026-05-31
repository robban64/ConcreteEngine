using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxShaders
{
    public static readonly Dictionary<uint, string> UniformSamplerByHash = new(16);

    private readonly GfxResourceDisposer _disposer;
    private readonly IDriverDebugger _debugger;

    private readonly ShaderStore _store;
    private readonly GlShaders _driver;

    internal GfxShaders(GfxContextInternal context)
    {
        _store = GfxRegistry.GetGfxStore<ShaderMeta>();
        _driver = context.Driver.Shaders;
        _disposer = context.Disposer;
        _debugger = context.Driver.Debugger;
    }

    public ShaderId CreateShader(NativeView<byte> vs, NativeView<byte> fs, out GfxUniformSampler[] samplerInfo)
    {
        var programRef = _driver.CreateShader(vs, fs);

        var samplerList = new List<GfxUniformSampler>(4);
        _driver.GetSamplersFromProgram(programRef, samplerList);
        samplerInfo = samplerList.ToArray();

        var meta = new ShaderMeta(samplerInfo.Length);
        return _store.Add(in meta, programRef);
    }

    public void RecreateShader(ShaderId shaderId, NativeView<byte> vs, NativeView<byte> fs,
        out GfxUniformSampler[] samplers)
    {
        ArgumentOutOfRangeException.ThrowIfZero(shaderId.Id, nameof(shaderId));
        if (vs.IsNull || vs.Length == 0) throw new ArgumentOutOfRangeException(nameof(vs));
        if (fs.IsNull || fs.Length == 0) throw new ArgumentOutOfRangeException(nameof(fs));

        _debugger.ToggleDebug(false);
        GfxHandle oldRef, newRef;
        int samplerCount = 0;
        try
        {
            oldRef = _store.GetHandleAndMeta(shaderId, out var oldMeta);
            newRef = _driver.CreateShader(vs, fs);
            samplerCount = oldMeta.SamplerSlots;
        }
        finally
        {
            _debugger.ToggleDebug(true);
        }

        var samplerList = new List<GfxUniformSampler>(samplerCount);
        _driver.GetSamplersFromProgram(newRef, samplerList);
        samplers = samplerList.ToArray();

        var meta = new ShaderMeta(samplers.Length);
        _store.Replace(shaderId, in meta, newRef, out _);

        _disposer.EnqueueReplace(oldRef);
    }
}