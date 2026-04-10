using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Renderer.Data;
using static ConcreteEngine.Renderer.Data.RenderLimits;

namespace ConcreteEngine.Renderer.Buffer;

public sealed class MaterialBuffer : IDisposable
{
    private const int DefaultTextureSlotCapacity = DefaultMaterialBufferCapacity * 4;

    private int _count;
    private int _slotIdx;
    private bool _hasDrained;

    private RangeU16[] _slotRanges = new RangeU16[DefaultMaterialBufferCapacity];
    private TextureBinding[] _textureSlots = new TextureBinding[DefaultTextureSlotCapacity];
    private RenderMaterialMeta[] _metas = new RenderMaterialMeta[DefaultMaterialBufferCapacity];

    private NativeArray<MaterialUniformRecord> _buffer =
        NativeArray.Allocate<MaterialUniformRecord>(DefaultMaterialBufferCapacity);

    internal MaterialBuffer() { }

    public int Count => _count;
    public bool HasDrained => _hasDrained;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<TextureBinding> GetMetaAndSlots(MaterialId materialId, out RenderMaterialMeta meta)
    {
        var index = materialId.Index();
        meta = _metas[index];
        var range = _slotRanges[index];
        return _textureSlots.AsSpan(range.Offset, range.Length);
    }

    public void Submit(in RenderMaterialPayload payload, ReadOnlySpan<TextureBinding> slots)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slots.Length, TextureSlots);

        var index = payload.MaterialId.Index();

        EnsureCapacity(index + 1);
        EnsureTextureSlotCapacity(slots.Length);

        payload.WriteTo(ref _metas[index], ref _buffer[index]);

        var slotIdx = _slotIdx;
        for (var i = 0; i < slots.Length; i++, slotIdx++)
            _textureSlots[slotIdx] = slots[i];

        _slotRanges[index] = new RangeU16((ushort)_slotIdx, (ushort)slots.Length);

        _count++;
        _slotIdx = slotIdx;
    }

    internal NativeView<MaterialUniformRecord> DrainDrawMaterialData()
    {
        InvalidOpThrower.ThrowIf(_hasDrained);
        InvalidOpThrower.ThrowIfNot(_metas.Length == _buffer.Length);

        if (_count == 0) return NativeView<MaterialUniformRecord>.MakeNull();

        _hasDrained = true;
        return _buffer.Slice(0, _count);
    }

    internal void Reset()
    {
        _slotIdx = 0;
        _count = 0;
        _hasDrained = false;
    }

    private void EnsureCapacity(int amount)
    {
        if (_metas.Length > amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_metas.Length, amount, MaxTextureSlotBuffCapacity);

        if (newCap > MaxMaterialBufferCapacity)
            ThrowMaxCapacityExceeded();

        Console.WriteLine($"{nameof(MaterialBuffer)} TextureSlots resize");
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

        Console.WriteLine($"{nameof(MaterialBuffer)} TextureSlots resize");
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