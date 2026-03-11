using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Renderer.Data;
using static ConcreteEngine.Renderer.Data.RenderLimits;

namespace ConcreteEngine.Renderer.Draw;

internal sealed class MaterialDrawBuffer : IDisposable
{
    private const int DefaultTextureSlotCapacity = DefaultMaterialBufferCapacity * 4;

    private RangeU16[] _slotRanges = new RangeU16[DefaultMaterialBufferCapacity];
    private TextureBinding[] _textureSlots = new TextureBinding[DefaultTextureSlotCapacity];
    private RenderMaterialMeta[] _metas = new RenderMaterialMeta[DefaultMaterialBufferCapacity];

    private NativeArray<MaterialUniformRecord> _buffer = NativeArray.Allocate<MaterialUniformRecord>(DefaultMaterialBufferCapacity);

    private int _idx;
    private int _slotIdx;
    private bool _hasDrained;

    public int Count => _idx;
    public bool HasDrained => _hasDrained;

    internal MaterialDrawBuffer()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<TextureBinding> GetMetaAndSlots(MaterialId materialId, out RenderMaterialMeta meta)
    {
        var index = materialId.Index();
        meta = _metas[index];
        var range = _slotRanges[index];
        return _textureSlots.AsSpan(range.Offset, range.Length);
    }


    public void SubmitDrawData(in RenderMaterialPayload payload, ReadOnlySpan<TextureBinding> slots)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slots.Length, TextureSlots);

        var index = payload.MaterialId.Index();

        EnsureCapacity(index + 1);
        EnsureTextureSlotCapacity(slots.Length);

        ref var buffer = ref _buffer[index];
        ref var meta = ref _metas[index];

        payload.WriteTo(ref meta, ref buffer);

        var slotIdx = _slotIdx;
        for (var i = 0; i < slots.Length; i++, slotIdx++)
            _textureSlots[slotIdx] = slots[i];

        _slotRanges[index] = new RangeU16((ushort)_slotIdx, (ushort)slots.Length);

        _idx++;
        _slotIdx = slotIdx;
    }

    internal ReadOnlySpan<MaterialUniformRecord> DrainDrawMaterialData()
    {
        InvalidOpThrower.ThrowIf(_hasDrained);
        InvalidOpThrower.ThrowIfNot(_metas.Length == _buffer.Length);

        if (_idx == 0) return ReadOnlySpan<MaterialUniformRecord>.Empty;

        _hasDrained = true;
        return _buffer.AsSpan(0, _idx);
    }

    internal void Reset()
    {
        _slotIdx = 0;
        _idx = 0;
        _hasDrained = false;
    }

    private void EnsureCapacity(int amount)
    {
        if (_metas.Length > amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_metas.Length, amount, MaxTextureSlotBuffCapacity);

        if (newCap > MaxMaterialBufferCapacity)
            ThrowMaxCapacityExceeded();

        Console.WriteLine($"{nameof(MaterialDrawBuffer)} TextureSlots resize");
        Array.Resize(ref _metas, newCap);
        Array.Resize(ref _slotRanges, newCap);
        _buffer.Resize(newCap, true);
    }

    private void EnsureTextureSlotCapacity(int amount)
    {
        if (_textureSlots.Length > amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_textureSlots.Length, amount, MaxTextureSlotBuffCapacity);
        if (newCap > MaxTextureSlotBuffCapacity)
            ThrowMaxCapacityExceeded();

        Console.WriteLine($"{nameof(MaterialDrawBuffer)} TextureSlots resize");
        Array.Resize(ref _textureSlots, newCap);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    [StackTraceHidden]
    private static void ThrowMaxCapacityExceeded() =>
        throw new OutOfMemoryException("Material Buffer exceeded max limit");

    public void Dispose()
    {
        _buffer.Dispose();
    }
}