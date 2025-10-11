using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Core.Rendering.Registry;

//TODO
public sealed class RenderRegistryBuilder
{
    private UboRegistryBuilder? _uboBuilder;
    private FboRegistryBuilder? _fboBuilder;
    private ShaderRegistryBuilder? _shaderBuilder;
    
    private readonly RenderUboRegistry _uboRegistry;
    private readonly RenderFboRegistry _fboRegistry;
    private readonly RenderShaderRegistry _shaderRegistry;

    internal RenderRegistryBuilder( RenderUboRegistry uboRegistry, RenderFboRegistry fboRegistry,
        RenderShaderRegistry shaderRegistry)
    {
        _uboRegistry = uboRegistry;
        _fboRegistry = fboRegistry;
        _shaderRegistry = shaderRegistry;
    }

    public UboRegistryBuilder BuildUniformBuffers(Func<UboRegistryBuilder, UboRegistryBuilder> builder)
    {
        InvalidOpThrower.ThrowIfNotNull(_uboBuilder, nameof(_uboBuilder));
        return _uboBuilder = builder(new UboRegistryBuilder(_uboRegistry));
    }

    public FboRegistryBuilder BuildFrameBuffers(Func<FboRegistryBuilder, FboRegistryBuilder> builder)
    {
        InvalidOpThrower.ThrowIfNotNull(_fboBuilder, nameof(_fboBuilder));
        return _fboBuilder = builder(new FboRegistryBuilder(_fboRegistry));
    }
    
    public ShaderRegistryBuilder BuildShaders(Func<ShaderRegistryBuilder, ShaderRegistryBuilder> builder)
    {
        InvalidOpThrower.ThrowIfNotNull(_shaderBuilder, nameof(_shaderBuilder));
        return _shaderBuilder = builder(new ShaderRegistryBuilder(_shaderRegistry));
    }
    

    public sealed class UboRegistryBuilder
    {
        private readonly RenderUboRegistry _uboRegistry;
        internal UboRegistryBuilder(RenderUboRegistry uboRegistry) => _uboRegistry = uboRegistry;

        public void RegisterUbo<TUbo>() where TUbo : unmanaged, IStd140Uniform => _uboRegistry.Register<TUbo>();
    }

    public sealed class FboRegistryBuilder
    {
        private readonly RenderFboRegistry _fboRegistry;
        internal FboRegistryBuilder(RenderFboRegistry fboRegistry) => _fboRegistry = fboRegistry;

        public void Register<TTag>(FboVariant variant, RegisterFboEntry entry) where TTag : unmanaged, IRenderPassTag =>
            _fboRegistry.Register<TTag>(variant, entry);
    }
    
    public sealed class ShaderRegistryBuilder
    {
        
        private readonly RenderShaderRegistry _shaderRegistry;

        internal ShaderRegistryBuilder(RenderShaderRegistry shaderRegistry) => _shaderRegistry = shaderRegistry;
    }
}