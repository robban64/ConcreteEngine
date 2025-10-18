using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Data;
using static ConcreteEngine.Core.Rendering.Data.RenderLimits;

namespace ConcreteEngine.Core.Rendering.Draw;

internal sealed class MaterialDrawBuffer
{
    private const int MaxTextureSlotCapacity = MaxMaterialBufferCapacity * TextureSlots;
    private const int DefaultTextureSlotCapacity = DefaultMaterialBufferCapacity * TextureSlots;

    private int _idx = 0;
    private int _slotIdx = 0;
    private bool _hasDrained = false;

    private RangeU16[] _slotRanges = new RangeU16[DefaultMaterialBufferCapacity];
    private TextureSlotInfo[] _textureSlots = new TextureSlotInfo[DefaultTextureSlotCapacity];

    private DrawMaterialMeta[] _metas = new DrawMaterialMeta[DefaultMaterialBufferCapacity];
    private MaterialUniformRecord[] _buffer = new MaterialUniformRecord[DefaultMaterialBufferCapacity];

    public int Count => _idx;
    public bool HasDrained => _hasDrained;

    internal MaterialDrawBuffer()
    {
    }

    internal ReadOnlySpan<DrawMaterialMeta> MaterialMetas => _metas;
    internal DrawMaterialMeta GetMeta(MaterialId materialId) => _metas[materialId - 1];

    internal ReadOnlySpan<TextureSlotInfo> GetMetaAndSlots(MaterialId materialId, out DrawMaterialMeta meta)
    {
        meta = _metas[materialId - 1];
        var range = _slotRanges[materialId - 1];
        return _textureSlots.AsSpan(range.Offset, range.Length);
    }


    public void SubmitDrawData(in DrawMaterialPayload payload, ReadOnlySpan<TextureSlotInfo> slots)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slots.Length, TextureSlots);
        EnsureCapacity(1);
        EnsureTextureSlotCapacity(slots.Length);

        ref readonly var data = ref payload;
        _buffer[_idx] = new MaterialUniformRecord(in data.MatParams);
        _metas[_idx] = data.Meta;

        var slotIdx = _slotIdx;
        for (var i = 0; i < slots.Length; i++, slotIdx++)
            _textureSlots[slotIdx] = slots[i];

        _slotRanges[_idx] = new RangeU16((ushort)_slotIdx, (ushort)slots.Length);

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
/*
    public void UploadTextureSlotData(ReadOnlySpan<TextureSlotInfo> payload)
    {
        var count = payload.Length;
        if (count == 0) return;

        var slotIdx = _slotIdx;
        for (var i = 0; i < count; i++, slotIdx++)
        {
            _textureSlots[_slotIdx] = payload[i];
        }

        _slotIdx = slotIdx;
    }

    public void SubmitDrawData(ReadOnlySpan<DrawMaterialPayload> payload)
    {
        var count = payload.Length;
        if (count == 0) return;

        EnsureCapacity(payload.Length);

        for (var i = 0; i < count; i++)
        {
            ref readonly var data = ref payload[i];
            _buffer[i] = new MaterialUniformRecord(in data.MatParams);
            _metas[i] = data.Meta;
        }

        _idx = count;
    }

    internal void DispatchMaterials()
    {
        Debug.Assert(_metas.Length == _buffer.Length);
        if (_idx == 0) return;
        if (_idx == 1)
        {
            _drawUniforms.UploadMaterialRecord(_metas[0].MaterialId, in _buffer[0]);
            return;
        }

        var commands = _metas.AsSpan(0, _idx);
        var payloads = _buffer.AsSpan(0, _idx);
        _drawUniforms.UploadMaterial(commands, payloads);
    }
*/

    private void EnsureCapacity(int amount)
    {
        if (_metas.Length >= amount) return;
        var newCap = ArrayUtility.CapacityGrowthToFit(amount, Math.Max(amount, 4));

        if (newCap > MaxMaterialBufferCapacity)
            ThrowMaxCapacityExceeded();

        Array.Resize(ref _metas, newCap);
        Array.Resize(ref _buffer, newCap);
    }

    private void EnsureTextureSlotCapacity(int amount)
    {
        if (_textureSlots.Length >= amount) return;
        var newCap = ArrayUtility.CapacityGrowthToFit(amount, Math.Max(amount, 4));

        if (newCap > MaxTextureSlotCapacity)
            ThrowMaxCapacityExceeded();

        Array.Resize(ref _textureSlots, newCap);
    }

    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn, StackTraceHidden]
    private static void ThrowMaxCapacityExceeded() =>
        throw new OutOfMemoryException("Material Buffer exceeded max limit");
}