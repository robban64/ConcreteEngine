using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxShaders
{
    private readonly GfxResourceDisposer _disposer;

    internal GfxShaders(GfxContextInternal context)
    {
        _disposer = context.Disposer;
    }

    public ShaderId CreateShader(NativeView<byte> vs, NativeView<byte> fs, out GfxUniformSampler[] samplerInfo)
    {
        var programRef = GlShaders.CreateShader(vs, fs);

        var samplerList = new List<GfxUniformSampler>(4);
        GlShaders.GetSamplersFromProgram(programRef, samplerList);
        samplerInfo = samplerList.ToArray();

        var meta = new ShaderMeta(samplerInfo.Length);
        return GfxRegistry.ShaderStore.Add(in meta, programRef);
    }

    public void RecreateShader(ShaderId shaderId, NativeView<byte> vs, NativeView<byte> fs,
        out GfxUniformSampler[] samplers)
    {
        ArgumentOutOfRangeException.ThrowIfZero(shaderId.Id, nameof(shaderId));
        if (vs.IsNull || vs.Length == 0) throw new ArgumentOutOfRangeException(nameof(vs));
        if (fs.IsNull || fs.Length == 0) throw new ArgumentOutOfRangeException(nameof(fs));

        GlBackendDriver.ToggleDebug(false);
        GfxHandle oldHandle, newHandle;
        int samplerCount = 0;
        try
        {
            oldHandle = GfxRegistry.ShaderStore.GetHandleAndMeta(shaderId, out var oldMeta);
            newHandle = GlShaders.CreateShader(vs, fs);
            samplerCount = oldMeta.SamplerSlots;
        }
        finally
        {
            GlBackendDriver.ToggleDebug(true);
        }

        var samplerList = new List<GfxUniformSampler>(samplerCount);
        GlShaders.GetSamplersFromProgram(newHandle, samplerList);
        samplers = samplerList.ToArray();

        var meta = new ShaderMeta(samplers.Length);
        GfxRegistry.ShaderStore.Replace(shaderId, in meta, newHandle);
        _disposer.EnqueueReplace(shaderId, oldHandle);
    }
}