using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderUbo(UniformBufferId id, UboSlot slot, in UniformBufferMeta meta)
{
    public UniformBufferId Id { get; } = id;
    public UboSlot Slot { get; } = slot;
    public uint Stride { get; } = (uint)meta.Stride;
    public uint Capacity { get; private set; } = meta.Capacity;

    private uint _uploadCursor = 0;
    private uint _drawCursor = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetCursor()
    {
        _uploadCursor = 0;
        _drawCursor = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasNextCapacity() => _uploadCursor + Stride < Capacity;

    public void SetCapacity(uint capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, Stride);
        InvalidOpThrower.ThrowIf(_uploadCursor > 0 || _drawCursor > 0);
        Capacity = capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint NextUploadCursor()
    {
        InvalidOpThrower.ThrowIfNot(HasNextCapacity());

        var offset = _uploadCursor;
        _uploadCursor += Stride;
        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint SetUploadCursor(uint idx)
    {
        _uploadCursor = idx * Stride;
        Debug.Assert(_uploadCursor < Capacity, "Ubo overflow. Increase capacity.");
        return _uploadCursor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint NextDrawCursor()
    {
        var offset = _drawCursor;
        _drawCursor += Stride;
        Debug.Assert(_drawCursor < Capacity, "Ubo overflow. Increase capacity.");
        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint SetDrawCursor(int idx)
    {
        _drawCursor = (uint)idx * Stride;
        Debug.Assert(_drawCursor < Capacity, "Ubo overflow. Increase capacity.");
        return _drawCursor;
    }

    public uint GetCapacityFor(int records)
    {
        uint required = Stride * (uint)int.Max(1, records);
        if (required <= Capacity) return 0;
        return UniformBufferUtils.NextCapacity(Capacity, required);
    }
}