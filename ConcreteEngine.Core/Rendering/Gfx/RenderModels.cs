using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

namespace ConcreteEngine.Core.Rendering.Gfx;

public readonly struct FboRenderData(Size2D size, FboAttachmentIds attachments, int samples)
{
    public readonly Size2D Size = size;
    public readonly FboAttachmentIds Attachments = attachments;
    public readonly int Samples = samples;
}
public delegate FboRenderData FboRenderDataDel(FrameBufferId fboId); 

public sealed class RenderFbo
{
    public FrameBufferId FboId { get;  }

    private readonly FboRenderDataDel _dataCallback;
    public RenderFbo(FrameBufferId fboId, FboRenderDataDel dataCallback)
    {
        FboId = fboId;
        _dataCallback  = dataCallback;
    }

    public FboRenderData RenderData => _dataCallback(FboId);
    
    public required Size2D OutputSize { get; set; }
    public Vector2 CalculateRatio { get; set; } = Vector2.One;

    public Size2D? FixedSize { get; set; }
    public CalcFboSizeDel? CalculateSize { get; set; }
}

public readonly record struct UboSlot(int Value);

public readonly record struct UboSlotRef<T>(UboSlot Slot) where T : unmanaged, IUniformGpuData
{
    internal static UboSlotRef<T> Make(int value) => new(new UboSlot(value));
}


public sealed class RenderUbo
{
    public required UniformBufferId Id { get; init; }
    public required UniformGpuSlot Slot { get; init; }
    
    public 
    
    public UboArena? UboBufferArena { get; set; }
    

}

public sealed class RenderShader
{
    public required FrameBufferId Id { get; init; }
    private readonly int[] _locations;
    private readonly Dictionary<string, int> _uniforms;
    
    public IReadOnlyDictionary<string, int> Uniforms => _uniforms;

    public RenderShader(List<(string, int)> uniformPairs)
    {
        _locations = new int [GraphicsEnumCache.ShaderUniformVals.Length];
        _uniforms = new Dictionary<string, int>(uniformPairs.Count);
        
        foreach (var (uniform, location) in uniformPairs)
        {
            _uniforms.Add(uniform, location);
            var idx = uniform.IndexOf(".", StringComparison.Ordinal);
        }

        for (int i = 0; i < _locations.Length; i++)
        {
            var uniformName = GraphicsEnumCache.ShaderUniformVals[i].ToUniformName();
            if (_uniforms.TryGetValue(uniformName, out var uniformLocation))
            {
                _locations[i] = uniformLocation;
                continue;
            }

            _locations[i] = -1;
        }
    }

    public int this[ShaderUniform uniform]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _locations[(int)uniform];
    }
}