using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Registry;

internal sealed class RenderShaderRegistry
{
    private readonly GfxShaders _gfxShaders;
    private readonly GfxResourceApi _gfxApi;

    private RenderShader[] _shaderRegistry = Array.Empty<RenderShader>();
    private int _shaderCount = 0;

    internal RenderShaderRegistry(GfxContext gfx)
    {
        _gfxApi = gfx.ResourceContext.ResourceManager.GetGfxApi();
        _gfxShaders = gfx.Shaders;
    }
    

    public void RegisterCollection(ReadOnlySpan<ShaderId> shaders)
    {
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

    public RenderShader GetRenderShader(ShaderId shaderId) => _shaderRegistry[shaderId - 1];
}