using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics;

public sealed class GraphicsResourceBuilder
{
    internal delegate(GlUniformBufferHandle, UniformBufferMeta) UboFactory(GlShaderFactory factory);
    
    private readonly Dictionary<UniformGpuSlot, UboFactory> _uboBuilder = new(4);
    
    internal IReadOnlyCollection<UboFactory> UboBuilder => _uboBuilder.Values;

    internal void Clear()
    {
        _uboBuilder.Clear();
    }

    
    public void RegisterFbo<T>() where T : unmanaged, IUniformGpuData
    {
        
    }
    
    public void RegisterUbo<T>(UniformGpuSlot slot, UboDefaultCapacity capacity) where T : unmanaged, IUniformGpuData
    {
        _uboBuilder.Add(slot, (factory) =>
        {
            var handle = factory.CreateUniformBuffer<T>(slot, capacity, out var meta);
            return (handle, meta);
        });
    }
}