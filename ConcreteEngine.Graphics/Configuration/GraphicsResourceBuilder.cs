using System.Numerics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;

public sealed class GraphicsResourceBuilder
{
    internal delegate(GlUniformBufferHandle, UniformBufferMeta) UboFactory(GlShaderFactory factory);
    
    private readonly Dictionary<UniformGpuSlot, UboFactory> _uboBuilder = new(4);
    private readonly List<IFrameBufferDescriptor> _fboBuilder = new(4);
    
    internal IReadOnlyDictionary<UniformGpuSlot, UboFactory>  UboBuilder => _uboBuilder;
    internal IReadOnlyList<IFrameBufferDescriptor>  FboBuilder => _fboBuilder;

    internal void Clear()
    {
        _uboBuilder.Clear();
    }

    
    public void RegisterFbo<T>(T t = default) where T : unmanaged, IFrameBufferDescriptor
    {
        _fboBuilder.Add(t);
    }
    
    public void RegisterUbo<T>(UniformGpuSlot slot, UboDefaultCapacity capacity) where T : unmanaged, IUniformGpuData
    {
        ArgumentOutOfRangeException.ThrowIfEqual(UniformBufferUtils.IsStd140Aligned<T>(), false);
        _uboBuilder.Add(slot, (factory) =>
        {
            var id = factory.CreateUniformBuffer<T>(slot, capacity, out var meta);
            return (id, meta);
        });
    }

    
    public static class FboSizing
    {
        public static Vector2D<int> ResolveEffectiveSize(
            Vector2D<int> viewSize, Vector2 downscaleRatio, Vector2D<int>? absoluteSize)
        {
            var rX = (downscaleRatio.X <= 0f || downscaleRatio.X > 1f) ? 1f : downscaleRatio.X;
            var rY = (downscaleRatio.Y <= 0f || downscaleRatio.Y > 1f) ? 1f : downscaleRatio.Y;

            var baseSize = (absoluteSize is null || absoluteSize.Value == default)
                ? viewSize : absoluteSize.Value;

            var w = MathF.Max(1, MathF.Floor(baseSize.X * rX));
            var h = MathF.Max(1, MathF.Floor(baseSize.Y * rY));
            return new Vector2D<int>((int)w, (int)h);
        }

        // Normalize requested samples: <=1 => 1 (no MSAA).
        public static int ResolveSamples(int samples) => samples <= 1 ? 1 : samples;
    }
}