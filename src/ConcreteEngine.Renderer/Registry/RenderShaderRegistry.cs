using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Renderer.Registry;


public sealed class RenderShaderRegistry
{
    private readonly GfxShaders _gfxShaders;
    private readonly GfxResourceApi _gfxApi;

    private int _count = 0;

    private RenderShader[] _shaderRegistry = [];

    private RenderCoreShaders _coreShaders;

    internal RenderShaderRegistry(GfxContext gfx)
    {
        _gfxApi = gfx.ResourceManager.GetGfxApi();
        _gfxShaders = gfx.Shaders;
    }

    public ref readonly RenderCoreShaders CoreShaders => ref _coreShaders;

    public RenderShader GetRenderShader(ShaderId shaderId) => _shaderRegistry[shaderId - 1];

    internal void FinishRegistration()
    {
        GetRenderShader(_coreShaders.HighlightShader).UsePlainUniforms(_gfxShaders);
        GetRenderShader(_coreShaders.BoundingBoxShader).UsePlainUniforms(_gfxShaders);
    }

    internal void RegisterCollection(Span<ShaderId> shaders)
    {
        InvalidOpThrower.ThrowIf(_count > 0, nameof(_count));

        _shaderRegistry = new RenderShader[shaders.Length];
        _count = shaders.Length;

        foreach (var shaderId in shaders)
        {
            if (_shaderRegistry[shaderId - 1] != null)
                throw new InvalidOperationException(nameof(_shaderRegistry));

            var meta = _gfxApi.GetMeta<ShaderId, ShaderMeta>(shaderId);
            _shaderRegistry[shaderId - 1] = new RenderShader(shaderId, meta);
        }
    }

    internal void RegisterCollection(IReadOnlyList<ShaderId> shaders)
    {
        InvalidOpThrower.ThrowIf(_count > 0, nameof(_count));

        _shaderRegistry = new RenderShader[shaders.Count];
        _count = shaders.Count;
        //var uniforms = _gfxShaders.GetUniformList(shaderId);

        foreach (var shaderId in shaders)
        {
            if (_shaderRegistry[shaderId - 1] != null)
                throw new InvalidOperationException(nameof(_shaderRegistry));

            var meta = _gfxApi.GetMeta<ShaderId, ShaderMeta>(shaderId);
            _shaderRegistry[shaderId - 1] = new RenderShader(shaderId, meta);
        }
    }


    internal void RegisterCoreShader(in RenderCoreShaders shaders)
    {
        InvalidOpThrower.ThrowIf(_count == 0, nameof(_count));
        _coreShaders = shaders;
    }
}