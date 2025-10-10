#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

public sealed class RenderUbo
{
    public UniformBufferId Id { get; }
    public UboSlot Slot { get; }
    public nint Stride { get; }
    public nint Capacity { get; private set; }

    private nint _uploadCursor;
    private nint _drawCursor;


    public RenderUbo(UniformBufferId id, UboSlot slot, in UniformBufferMeta meta)
    {
        Id = id;
        Slot = slot;
        Stride = meta.Stride;
        Capacity = meta.Capacity;

        _uploadCursor = 0;
        _drawCursor = 0;
    }

    public void ResetCursor()
    {
        _uploadCursor = 0;
        _drawCursor = 0;
    }

    public void SetCapacity(nint capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, Stride, nameof(capacity));
        InvalidOpThrower.ThrowIf(_uploadCursor > 0 || _drawCursor > 0);
        Capacity = capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nint NextUploadCursor()
    {
        bool overflow = _uploadCursor + Stride > Capacity;
        Debug.Assert(!overflow, "UboRing overflow. Increase capacity.");

        var offset = _uploadCursor;
        _uploadCursor += Stride;
        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nint SetUploadCursor(int idx)
    {
        _uploadCursor = idx * Stride;

        bool overflow = _uploadCursor > Capacity;
        Debug.Assert(!overflow, "UboRing overflow. Increase capacity.");
        return _uploadCursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nint NextDrawCursor()
    {
        bool overflow = _drawCursor + Stride > Capacity;
        Debug.Assert(!overflow, "UboRing overflow. Increase capacity.");

        var offset = _drawCursor;
        _drawCursor += Stride;
        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nint SetDrawCursor(int idx)
    {
        _drawCursor = idx * Stride;

        bool overflow = _drawCursor > Capacity;
        Debug.Assert(!overflow, "UboRing overflow. Increase capacity.");
        return _drawCursor;
    }

    public nint GetCapacityFor(int expectedRecords)
    {
        nint required = Stride * Math.Max(1, expectedRecords);
        if (required <= Capacity) return 0;
        return UniformBufferUtils.NextCapacity(Capacity, required);
    }
}