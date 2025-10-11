using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Registry;

internal sealed class RenderShaderRegistry
{
    private readonly GfxShaders _gfxShaders;

    private RenderShader[] _shaderRegistry = Array.Empty<RenderShader>();
    private int _shaderCount = 0;

    internal RenderShaderRegistry(GfxContext gfx)
    {
        _gfxShaders = gfx.Shaders;
    }
    

    public void RegisterCollection(ReadOnlySpan<ShaderId> shaders)
    {
        _shaderRegistry = new RenderShader[shaders.Length];
        _shaderCount = shaders.Length;
        
        foreach (var shaderId in shaders)
        {
            var uniforms = _gfxShaders.GetUniformList(shaderId);
            if (_shaderRegistry[shaderId - 1] != null) throw new InvalidOperationException(nameof(_shaderRegistry));
            _shaderRegistry[shaderId - 1] = new RenderShader(shaderId, uniforms);
        }
    }

    public RenderShader GetRenderShader(ShaderId shaderId) => _shaderRegistry[shaderId - 1];
}