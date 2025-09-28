#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Utility;

public sealed class UboArena
{
    private readonly nint _blockSize;
    private readonly nint _stride;

    private nint _capacity;

    private nint _uploadCursor;
    private nint _drawCursor;

    public nint Capacity => _capacity;
    public nint Stride => _stride;
    public nint BlockSize => _blockSize;

    public UboArena(in UniformBufferMeta meta)
    {
        _stride = meta.Stride;
        _blockSize = meta.BlockSize;

        Debug.Assert(_stride >= _blockSize);
    }

    public void Prepare(nint capacity)
    {
        _capacity = capacity;
        _uploadCursor = 0;
        _drawCursor = 0;
    }

    public nint GetCapacityFor(int expectedRecords)
    {
        nint required = _stride * (nint)Math.Max(1, expectedRecords);
        if (required <= _capacity) return 0;
        return UniformBufferUtils.NextCapacity(_capacity, required);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nint NextUploadCursor()
    {
        bool overflow = _uploadCursor + _stride > _capacity;
        Debug.Assert(!overflow, "UboRing overflow. Increase capacity.");

        var offset = _uploadCursor;
        _uploadCursor += _stride;
        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nint NextDrawCursor()
    {
        bool overflow = _drawCursor + _stride > _capacity;
        Debug.Assert(!overflow, "UboRing overflow. Increase capacity.");

        var offset = _drawCursor;
        _drawCursor += _stride;
        return offset;
    }
}