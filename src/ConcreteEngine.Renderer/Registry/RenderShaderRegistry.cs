using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderShaderRegistry
{
    private static CoreShaders _coreShaders;

    private int _count;

    private RenderShader[] _shaderRegistry = [];


    internal RenderShaderRegistry() { }

    public static ref readonly CoreShaders CoreShaders => ref _coreShaders;

    public RenderShader GetRenderShader(ShaderId shaderId) => _shaderRegistry[shaderId - 1];

    internal void FinishRegistration() { }

    internal void RegisterCollection(ShaderId[] shaders)
    {
        if (_count > 0) Throwers.InvalidOperation(nameof(_count));

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
        if (_count == 0) Throwers.InvalidOperation(nameof(_count));
        _coreShaders = shaders;
    }
}