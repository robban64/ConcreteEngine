using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Renderer.Data;
using static ConcreteEngine.Renderer.Data.RenderLimits;

namespace ConcreteEngine.Renderer.Draw;

internal sealed class MaterialDrawBuffer
{
    private const int DefaultTextureSlotCapacity = DefaultMaterialBufferCapacity * 4;

    private RangeU16[] _slotRanges = new RangeU16[DefaultMaterialBufferCapacity];
    private TextureSlotInfo[] _textureSlots = new TextureSlotInfo[DefaultTextureSlotCapacity];

    private RenderMaterialMeta[] _metas = new RenderMaterialMeta[DefaultMaterialBufferCapacity];
    private MaterialUniformRecord[] _buffer = new MaterialUniformRecord[DefaultMaterialBufferCapacity];

    private int _idx = 0;
    private int _slotIdx = 0;
    private bool _hasDrained = false;

    public int Count => _idx;
    public bool HasDrained => _hasDrained;

    internal MaterialDrawBuffer()
    {
    }

    internal ReadOnlySpan<RenderMaterialMeta> MaterialMetas => _metas;
    internal RenderMaterialMeta GetMeta(MaterialId materialId) => _metas[materialId - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<TextureSlotInfo> GetMetaAndSlots(MaterialId materialId, out RenderMaterialMeta meta)
    {
        meta = _metas[materialId - 1];
        var range = _slotRanges[materialId - 1];
        return _textureSlots.AsSpan(range.Offset, range.Length);
    }


    public void SubmitDrawData(in RenderMaterialPayload payload, ReadOnlySpan<TextureSlotInfo> slots)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slots.Length, TextureSlots);

        var index = payload.Meta.MaterialId - 1;

        EnsureCapacity(index + 1);
        EnsureTextureSlotCapacity(slots.Length);

        payload.Material.WriteTo(ref _buffer[index]);
        _metas[index] = payload.Meta;

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
        Array.Resize(ref _buffer, newCap);
        Array.Resize(ref _slotRanges, newCap);
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
}