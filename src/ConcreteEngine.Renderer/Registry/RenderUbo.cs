using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Utility;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderUbo
{
    public UniformBufferId Id { get; }
    public UboSlot Slot { get; }
    public uint Stride { get; }
    public uint Capacity { get; private set; }

    private uint _uploadCursor;
    private uint _drawCursor;


    public RenderUbo(UniformBufferId id, UboSlot slot, in UniformBufferMeta meta)
    {
        Id = id;
        Slot = slot;
        Stride = (uint)meta.Stride;
        Capacity = meta.Capacity;

        _uploadCursor = 0;
        _drawCursor = 0;
    }

    public void ResetCursor()
    {
        _uploadCursor = 0;
        _drawCursor = 0;
    }

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