using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderShaderRegistry
{
    private int _count;
    private CoreShaders _coreShaders;

    private RenderShader[] _shaderRegistry = [];

    private readonly GfxShaders _gfxShaders;

    internal RenderShaderRegistry(GfxContext gfx)
    {
        _gfxShaders = gfx.Shaders;
    }

    public ref readonly CoreShaders CoreShaders => ref _coreShaders;

    public RenderShader GetRenderShader(ShaderId shaderId) => _shaderRegistry[shaderId - 1];

    internal void FinishRegistration() { }

    internal void RegisterCollection(ShaderId[] shaders)
    {
        InvalidOpThrower.ThrowIf(_count > 0, nameof(_count));

        _shaderRegistry = new RenderShader[shaders.Length];
        _count = shaders.Length;

        foreach (var shaderId in shaders)
        {
            if (_shaderRegistry[shaderId - 1] != null)
                throw new InvalidOperationException(nameof(_shaderRegistry));

            var meta = GfxResourceApi.GetMeta(shaderId);
            _shaderRegistry[shaderId - 1] = new RenderShader(shaderId, meta);
        }
    }

    internal void RegisterCoreShader(in CoreShaders shaders)
    {
        InvalidOpThrower.ThrowIf(_count == 0, nameof(_count));
        _coreShaders = shaders;
    }
}