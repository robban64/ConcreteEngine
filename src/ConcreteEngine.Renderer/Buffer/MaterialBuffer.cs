using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Renderer.Core;
using static ConcreteEngine.Renderer.RenderLimits;

namespace ConcreteEngine.Renderer.Buffer;

public sealed class MaterialBuffer : IDisposable
{
    private const int DefaultTextureSlotCapacity = DefaultMaterialBufferCapacity * 4;

    public int Count { get; private set; }
    public bool HasDrained { get; private set; }

    private int _slotCount;

    private RangeU16[] _slotRanges = new RangeU16[DefaultMaterialBufferCapacity];
    private TextureBinding[] _textureSlots = new TextureBinding[DefaultTextureSlotCapacity];
    private RenderMaterialMeta[] _metas = new RenderMaterialMeta[DefaultMaterialBufferCapacity];

    private NativeArray<MaterialUniform> _buffer =
        NativeArray.Allocate<MaterialUniform>(DefaultMaterialBufferCapacity);

    internal MaterialBuffer() { }


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

        var slotIdx = _slotCount;
        for (var i = 0; i < slots.Length; i++, slotIdx++)
            _textureSlots[slotIdx] = slots[i];

        _slotRanges[index] = new RangeU16((ushort)_slotCount, (ushort)slots.Length);

        Count++;
        _slotCount = slotIdx;
    }

    internal NativeView<MaterialUniform> DrainBuffer()
    {
        Debug.Assert(_metas.Length == _buffer.Length);
        if (HasDrained) Throwers.InvalidOperation("Material buffer already drained");

        if (Count == 0) return NativeView<MaterialUniform>.MakeNull();

        HasDrained = true;
        return _buffer.Slice(0, Count);
    }

    internal void Reset()
    {
        _slotCount = 0;
        Count = 0;
        HasDrained = false;
    }

    private void EnsureCapacity(int amount)
    {
        if (_metas.Length > amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_metas.Length, amount, MaxTextureSlotBuffCapacity);

        if (newCap > MaxMaterialBufferCapacity)
            Throwers.BufferOverflow(nameof(MaterialBuffer), newCap, MaxMaterialBufferCapacity);

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
            Throwers.BufferOverflow("MaterialTextureBuffer", newCap, MaxMaterialBufferCapacity);

        Console.WriteLine($"{nameof(MaterialBuffer)} TextureSlots resize");
        Array.Resize(ref _textureSlots, newCap);
    }

    public void Dispose() => _buffer.Dispose();
}