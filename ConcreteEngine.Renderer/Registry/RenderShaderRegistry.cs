#region

using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Renderer.Registry;

public interface IRenderShaderRegistry
{
    void RegisterCollection(Span<ShaderId> shaders);
    void RegisterCoreShader(in RenderCoreShaders shaders);
}

internal sealed class RenderShaderRegistry : IRenderShaderRegistry
{
    private readonly GfxShaders _gfxShaders;
    private readonly GfxResourceApi _gfxApi;

    private RenderShader[] _shaderRegistry = Array.Empty<RenderShader>();
    private int _shaderCount = 0;

    private RenderCoreShaders _coreShaders;

    internal RenderShaderRegistry(GfxContext gfx)
    {
        _gfxApi = gfx.ResourceContext.ResourceManager.GetGfxApi();
        _gfxShaders = gfx.Shaders;
    }

    public ref readonly RenderCoreShaders CoreShaders => ref _coreShaders;

    public RenderShader GetRenderShader(ShaderId shaderId) => _shaderRegistry[shaderId - 1];

    public void RegisterCollection(Span<ShaderId> shaders)
    {
        InvalidOpThrower.ThrowIf(_shaderCount > 0, nameof(_shaderCount));

        _shaderRegistry = new RenderShader[shaders.Length];
        _shaderCount = shaders.Length;
        //var uniforms = _gfxShaders.GetUniformList(shaderId);

        foreach (var shaderId in shaders)
        {
            if (_shaderRegistry[shaderId - 1] != null)
                throw new InvalidOperationException(nameof(_shaderRegistry));

            var meta = _gfxApi.GetMeta<ShaderId, ShaderMeta>(shaderId);
            _shaderRegistry[shaderId - 1] = new RenderShader(shaderId, meta);
        }
    }

    public void RegisterCollection(IReadOnlyList<ShaderId> shaders)
    {
        InvalidOpThrower.ThrowIf(_shaderCount > 0, nameof(_shaderCount));

        _shaderRegistry = new RenderShader[shaders.Count];
        _shaderCount = shaders.Count;
        //var uniforms = _gfxShaders.GetUniformList(shaderId);

        foreach (var shaderId in shaders)
        {
            if (_shaderRegistry[shaderId - 1] != null)
                throw new InvalidOperationException(nameof(_shaderRegistry));

            var meta = _gfxApi.GetMeta<ShaderId, ShaderMeta>(shaderId);
            _shaderRegistry[shaderId - 1] = new RenderShader(shaderId, meta);
        }
    }


    public void RegisterCoreShader(in RenderCoreShaders shaders)
    {
        InvalidOpThrower.ThrowIf(_shaderCount == 0, nameof(_shaderCount));
        _coreShaders = shaders;
    }
}