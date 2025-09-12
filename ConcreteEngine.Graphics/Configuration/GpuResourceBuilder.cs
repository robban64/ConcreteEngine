using System.Numerics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;

public sealed class GpuResourceBuilder
{
    private delegate void UboFactory(IGraphicsDevice graphics, UniformGpuSlot slot, UboDefaultCapacity defaultCapacity);

    private readonly Dictionary<UniformGpuSlot, (UboDefaultCapacity, UboFactory)> _uboBuilder = new(4);
    private readonly List<IFrameBufferDescriptor> _fboBuilder = new(4);

    
    public void RegisterFbo<T>(T t = default) where T : unmanaged, IFrameBufferDescriptor
    {
        _fboBuilder.Add(t);
    }

    public void RegisterUbo<T>(UniformGpuSlot slot, UboDefaultCapacity capacity) where T : unmanaged, IUniformGpuData
    {
        ArgumentOutOfRangeException.ThrowIfEqual(UniformBufferUtils.IsStd140Aligned<T>(), false);

        _uboBuilder.Add(slot, (capacity, Factory));
        return;

        static void Factory(IGraphicsDevice graphics, UniformGpuSlot slot,
            UboDefaultCapacity defaultCapacity) =>
            graphics.CreateUniformBuffer<T>(slot, defaultCapacity, out _);
    }

    internal void Apply(IGraphicsDevice graphics)
    {
        // ubo
        foreach (var (slot, item) in _uboBuilder)
        {
            var (capacity, factory) = item;
            factory(graphics, slot, capacity);
        }
        
        Clear();
    }
    
    private void Clear()
    {
        _uboBuilder.Clear();
        _fboBuilder.Clear();
        _uboBuilder.TrimExcess();
        _fboBuilder.TrimExcess();
    }


}