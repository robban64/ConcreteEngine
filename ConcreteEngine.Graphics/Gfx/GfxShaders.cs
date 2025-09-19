using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxShaders
{
    private readonly FrontendStoreHub _resources;
    private readonly GfxResourceRepository _repository;
    
    private readonly GfxShadersBackend _backend;

    internal GfxShaders(GfxContext context)
    {
        _backend = new GfxShadersBackend(context);
        _resources = context.Stores;
        _repository = context.Repositories;
    }

    public ShaderId CreateShader(string vs, string fs, out ShaderMeta meta, out List<(string, int)> uniforms)
    {
        var programRef = _backend.CreateShader(vs, fs, out var samples, out uniforms);
        meta = new ShaderMeta((uint)samples);
        var shaderId = _resources.ShaderStore.Add(in meta, programRef);
        _repository.ShaderRepository.Add(shaderId, in meta, uniforms);
        return shaderId;
    }

    private sealed class GfxShadersBackend
    {
        private readonly IGraphicsDriver _driver;

        internal GfxShadersBackend(GfxContext context)
        {
            _driver = context.Driver;
        }
    
        public GfxRefToken<ShaderId> CreateShader(string vertexSource, string fragmentSource, out int samples, out List<(string, int)> uniforms)
        {
            var programRef = _driver.Shaders.CreateShader(vertexSource, fragmentSource);
            _driver.Shaders.GetUniformsFromProgram(in programRef.Handle, out  uniforms, out  samples);
            return programRef;
        }
    }
}
