using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxShaders
{
    private readonly FrontendStoreHub _resources;
    private readonly GfxResourceRepository _repository;
    
    private readonly GfxShaderInvoker _invoker;

    internal GfxShaders(GfxContext context)
    {
        _invoker = new GfxShaderInvoker(context);
        _resources = context.Stores;
        _repository = context.Repositories;
    }
    
    public ShaderId CreateShader(string vs, string fs, out ShaderMeta meta, out List<(string, int)> uniforms)
    {
        var programRef = _invoker.CreateShader(vs, fs, out var samples, out uniforms);
        meta = new ShaderMeta((uint)samples);
        var shaderId = _resources.ShaderStore.Add(in meta, programRef);
        _repository.ShaderRepository.Add(shaderId, in meta, uniforms);
        return shaderId;
    }
}