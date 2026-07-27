using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx;
using static ConcreteEngine.Engine.Render.RenderLimits;

namespace ConcreteEngine.Engine.Render.Buffers;

public sealed class MaterialBuffer : IDisposable
{
    private const int DefaultTextureSlotCapacity = DefaultMaterialBufferCapacity * 4;

    public int Count { get; private set; }
    public bool HasDrained { get; private set; }

    private int _slotCount;

    private RangeU16[] _slotRanges = new RangeU16[DefaultMaterialBufferCapacity];
    private MaterialMeta[] _metas = new MaterialMeta[DefaultMaterialBufferCapacity];

    private NativeArray<TextureBinding> _textureSlots =
        NativeArray.Allocate<TextureBinding>(DefaultTextureSlotCapacity);

    private NativeArray<MaterialUniform> _buffer =
        NativeArray.Allocate<MaterialUniform>(DefaultMaterialBufferCapacity);

    internal MaterialBuffer() { }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe ReadOnlySpan<TextureBinding> GetMetaAndSlots(Id16<Material> materialId, out MaterialMeta meta)
    {
        var index = materialId.Index();
        var range = _slotRanges[index];
        
        meta = _metas[index];
        return new ReadOnlySpan<TextureBinding>(_textureSlots + range.Offset, range.Length);
    }

    public void SubmitBindings(Id16<Material> id, ReadOnlySpan<TextureBinding> slots)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slots.Length, TextureSlots);
        EnsureTextureSlotCapacity(slots.Length);

        var slotIdx = _slotCount;
        for (var i = 0; i < slots.Length; i++, slotIdx++)
            _textureSlots[slotIdx] = slots[i];
        
        _slotRanges[id.Index()] = new RangeU16(_slotCount, slots.Length);
        _slotCount = slotIdx;
    }

    public ref MaterialUniform Submit(
        Id16<Material> id,
        ShaderId shaderId,
        GfxDrawState drawState,
        GfxDrawFunctions drawFunctions,
        sbyte shadowMapBinding)
    {
        var index = id.Index();
        EnsureCapacity(id.Value);

        _metas[index] = new MaterialMeta(shaderId, drawState, drawFunctions, shadowMapBinding);

        Count = int.Max(Count, index);
        return ref _buffer[index];
    }


    internal NativeView<MaterialUniform> DrainBuffer()
    {
        Debug.Assert(_metas.Length == _buffer.Length);
        if (HasDrained) Throwers.InvalidOperation("Material buffer already drained");

        if (Count == 0) return NativeView<MaterialUniform>.MakeNull();

        HasDrained = true;
        return _buffer.Slice(0, Count);
    }

    internal void NewFrame()
    {
        HasDrained = false;
    }

    private void EnsureCapacity(int amount)
    {
        if (_metas.Length > amount) return;
        var newCap = CapacityUtils.CapacityGrowthToFit(_metas.Length, amount);

        if (newCap > MaxMaterialBufferCapacity)
            Throwers.BufferOverflow(nameof(MaterialBuffer), newCap, MaxMaterialBufferCapacity);

        Console.WriteLine($"{nameof(MaterialBuffer)} TextureSlots resize");
        Array.Resize(ref _metas, newCap);
        Array.Resize(ref _slotRanges, newCap);
        _buffer.Resize(newCap, true);
    }

    private void EnsureTextureSlotCapacity(int amount)
    {
        if (_textureSlots.Length > _slotCount + amount) return;
        var newCap = CapacityUtils.CapacityGrowthToFit(_textureSlots.Length, amount);
        if (newCap > MaxTextureSlotBuffCapacity)
            Throwers.BufferOverflow("MaterialTextureBuffer", newCap, MaxMaterialBufferCapacity);

        Console.WriteLine($"{nameof(MaterialBuffer)} TextureSlots resize");
        _textureSlots.Resize(newCap, true);
    }

    public void Dispose() => _buffer.Dispose();
}