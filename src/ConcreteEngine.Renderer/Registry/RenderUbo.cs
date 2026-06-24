using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderUbo(UniformBufferId id, UboSlot slot, in UniformBufferMeta meta)
{
    public readonly UniformBufferId Id = id;
    public readonly UboSlot Slot = slot;
    public readonly int Stride = meta.Stride;
    public int Capacity { get; private set; } = meta.Capacity;

    private int _uploadCursor = 0;
    private int _drawCursor = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetCursor()
    {
        _uploadCursor = 0;
        _drawCursor = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasNextCapacity() => _uploadCursor + Stride < Capacity;

    public void SetCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, Stride);
        if (_drawCursor > 0 || _uploadCursor > 0) Throwers.InvalidOperation("Cursor not zero");
        Capacity = capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextUploadCursor()
    {
        Debug.Assert(HasNextCapacity(), "Ubo overflow. Increase capacity.");

        var offset = _uploadCursor;
        _uploadCursor += Stride;
        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int SetUploadCursor(int idx)
    {
        _uploadCursor = idx * Stride;
        Debug.Assert(_uploadCursor < Capacity, "Ubo overflow. Increase capacity.");
        return _uploadCursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDrawCursor()
    {
        var offset = _drawCursor;
        _drawCursor += Stride;
        Debug.Assert(_drawCursor < Capacity, "Ubo overflow. Increase capacity.");
        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int SetDrawCursor(int idx)
    {
        _drawCursor = idx * Stride;
        Debug.Assert(_drawCursor < Capacity, "Ubo overflow. Increase capacity.");
        return _drawCursor;
    }

    public int GetCapacityFor(int records)
    {
        var required = Stride * int.Max(1, records);
        if (required <= Capacity) return 0;
        return UniformBufferUtils.NextCapacity(Capacity, required);
    }
}