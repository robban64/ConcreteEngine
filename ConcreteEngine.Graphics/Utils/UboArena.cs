using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Utils;

public sealed class UboArena
{
    private readonly UniformGpuSlot  _slot;
    private readonly nuint _blockSize;
    private readonly nuint _stride; 

    private nuint _capacity;

    private nuint _uploadCursor;
    private nuint _drawCursor;

    public UniformGpuSlot Slot => _slot;
    public nuint Capacity => _capacity;
    public nuint Stride   => _stride;
    public nuint BlockSize => _blockSize;

    internal UboArena(in UniformBufferMeta meta)
    {
        _slot    = meta.Slot;
        _stride  = meta.Stride;
        _blockSize = meta.BlockSize;
        
        Debug.Assert(_stride >= _blockSize);
    }

    public void Prepare(nuint capacity)
    {
        _capacity   = capacity;
        _uploadCursor = 0;
        _drawCursor = 0;
    }

    public nuint GetCapacityFor(int expectedRecords)
    {
        nuint required = _stride * (nuint)Math.Max(1, expectedRecords);
        if (required <= _capacity) return 0;
        return UniformBufferUtils.NextCapacity(_capacity, required);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint NextUploadCursor() 
    {
        bool overflow = _uploadCursor + _stride > _capacity;
        Debug.Assert(!overflow, "UboRing overflow. Increase capacity.");
        
        var offset = _uploadCursor;
        _uploadCursor += _stride;
        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint NextDrawCursor()
    {
        bool overflow = _drawCursor + _stride > _capacity;
        Debug.Assert(!overflow, "UboRing overflow. Increase capacity.");

        var offset = _drawCursor;
        _drawCursor += _stride;
        return offset;
    }

}