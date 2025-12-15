using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.OpenGL;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxBuffers
{
    private readonly GlBuffers _driverBuffer;

    private readonly VboStore _vboStore;
    private readonly IboStore _iboStore;
    private readonly UboStore _uboStore;

    private const BufferUsage DefaultUsage = BufferUsage.StaticDraw;

    internal GfxBuffers(GfxContextInternal context)
    {
        _driverBuffer = context.Driver.Buffers;
        _vboStore = context.Resources.GfxStoreHub.VboStore;
        _iboStore = context.Resources.GfxStoreHub.IboStore;
        _uboStore = context.Resources.GfxStoreHub.UboStore;
    }

    //BufferStorage.Dynamic, BufferAccess.MapWrite
    public VertexBufferId CreateVertexBuffer<T>(ReadOnlySpan<T> data, byte divisor, uint offset, BufferStorage storage,
        BufferAccess access, int defaultComponentCap = 0) where T : unmanaged
    {
        var stride = Unsafe.SizeOf<T>();
        var componentCount = data.Length;
        if (componentCount == 0 && defaultComponentCap > 0) componentCount = defaultComponentCap;
        var size = (uint)stride * (uint)componentCount;

        var meta = new VertexBufferMeta(stride, componentCount, offset, divisor, DefaultUsage, storage, access);

        var payload = data.Length > 0 ? MemoryMarshal.AsBytes(data) : ReadOnlySpan<byte>.Empty;
        var vboRef = _driverBuffer.CreateVertexBuffer(payload, new GfxBufferDataDesc(size, storage, access));

        return _vboStore.Add(meta, vboRef);
    }

    //BufferStorage.Static, BufferAccess.None
    public IndexBufferId CreateIndexBuffer<T>(ReadOnlySpan<T> data, BufferStorage storage, BufferAccess access,
        int defaultComponentCap = 0) where T : unmanaged
    {
        var stride = Unsafe.SizeOf<T>();
        var componentCount = data.Length;
        if (componentCount == 0 && defaultComponentCap > 0) componentCount = defaultComponentCap;
        var size = (uint)stride * (uint)componentCount;

        if (stride != 1 && stride != 2 && stride != 4)
            GraphicsException.ThrowInvalidType(typeof(T).Name, "Invalid elemental size");

        var meta = new IndexBufferMeta(componentCount, stride, DefaultUsage, storage, access);
        var iboRef = _driverBuffer.CreateIndexBuffer(MemoryMarshal.AsBytes(data),
            new GfxBufferDataDesc(size, storage, access));

        return _iboStore.Add(meta, iboRef);
    }

    //BufferStorage.Dynamic, BufferAccess.MapWrite
    public UniformBufferId CreateUniformBuffer<T>(
        UboSlot slot,
        BufferStorage storage = BufferStorage.Dynamic,
        BufferAccess access = BufferAccess.MapWrite) where T : unmanaged
    {
        if (!UniformBufferUtils.IsStd140Aligned<T>())
            throw GraphicsException.InvalidStd140Layout(Unsafe.SizeOf<T>());

        var blockSize = Unsafe.SizeOf<T>();
        var stride = (uint)UniformBufferUtils.AlignUp(blockSize, UniformBufferUtils.UboOffsetAlign);
        var meta = new UniformBufferMeta(slot, (int)stride, (nint)stride, BufferUsage.DynamicDraw,
            BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        var uboRef = _driverBuffer.CreateUniformBuffer(slot, new GfxBufferDataDesc(stride, storage, access));

        var uboId = _uboStore.Add(meta, uboRef);
        return uboId;
    }


    public void SetVertexBufferData<T>(VertexBufferId vboId, uint offset, ReadOnlySpan<T> data, BufferUsage usage)
        where T : unmanaged
    {
        var vboRef = _vboStore.GetRefAndMeta(vboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.Stride > 0)
            GraphicsException.ThrowInvalidBufferData(nameof(vboId), "Buffer is static");

        var (stride, size) = ToStrideAndSize<T>(data.Length);
        _driverBuffer.SetVertexBufferData(vboRef, MemoryMarshal.AsBytes(data), size, usage);

        var newMeta = VertexBufferMeta.CreateCopy(in meta, data.Length, stride, offset, usage);
        _vboStore.ReplaceMeta(vboId, in newMeta, out _);
    }

    public void SetIndexBufferData<T>(IndexBufferId iboId, ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged
    {
        var iboRef = _iboStore.GetRefAndMeta(iboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.Stride > 0)
            GraphicsException.ThrowInvalidBufferData(nameof(iboId), "Buffer is static");

        var (stride, size) = ToStrideAndSize<T>(data.Length);
        _driverBuffer.SetIndexBufferData(iboRef, MemoryMarshal.AsBytes(data), size, usage);

        var newMeta = IndexBufferMeta.CreateCopy(in meta, data.Length, stride, usage);
        _iboStore.ReplaceMeta(iboId, in newMeta, out _);
    }

    public void SetUniformBufferCapacity(UniformBufferId uboId, nint capacity)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(0, (int)capacity);
        var refToken = _uboStore.GetRefAndMeta(uboId, out var meta);
        if (meta.Capacity == capacity) return;
        var newMeta = UniformBufferMeta.MakeResizeCopy(in meta, capacity);
        _uboStore.ReplaceMeta(uboId, in newMeta, out _);
        _driverBuffer.ResizeUniformBuffer(refToken, capacity, BufferUsage.DynamicDraw);
    }

    public void ClearUniformBufferData(UniformBufferId uboId)
    {
        var refToken = _uboStore.GetRefAndMeta(uboId, out var meta);
        _driverBuffer.ResizeUniformBuffer(refToken, meta.Capacity, BufferUsage.DynamicDraw);
    }


    public void UploadVertexBuffer<T>(VertexBufferId vboId, ReadOnlySpan<T> data, int offsetElements)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetElements, data.Length);
        var (offset, size) = ToSizeAndOffset<T>(offsetElements, data.Length);

        var vboRef = _vboStore.GetRefHandle(vboId);
        _driverBuffer.UploadVertexBufferData(vboRef, MemoryMarshal.AsBytes(data), offset, size);
    }

    public void UploadIndexBuffer<T>(IndexBufferId iboId, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetElements, data.Length);
        var iboRef = _iboStore.GetRefHandle(iboId);
        var (offset, size) = ToSizeAndOffset<T>(offsetElements, data.Length);
        _driverBuffer.UploadIndexBufferData(iboRef, MemoryMarshal.AsBytes(data), offset, size);
    }


    public void UploadUniformGpuData<T>(UniformBufferId uboId, in T data, nint offset) where T : unmanaged
    {
        //UniformBufferUtils.IsStd140AlignedOrThrow<T>(out nint stride);
        var uboRef = _uboStore.GetRefHandle(uboId);

        var tSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in data), 1);
        var bytes = MemoryMarshal.AsBytes(tSpan);

        _driverBuffer.UploadUniformBufferData(uboRef, bytes, offset, Unsafe.SizeOf<T>());
    }

    public void UploadUniformGpuSpan<T>(UniformBufferId uboId, ReadOnlySpan<T> data, nint offset) where T : unmanaged
    {
        UniformBufferUtils.IsStd140AlignedOrThrow<T>(out nint stride);
        var uboRef = _uboStore.GetRefAndMeta(uboId, out var meta);
        var len = stride * data.Length;

        if (stride != meta.Stride)
            GraphicsException.ThrowInvalidBufferData(nameof(T), $"Invalid stride {stride},  expected {meta.Stride}");

        if (offset + len > meta.Capacity)
            GraphicsException.ThrowCapabilityExceeded(nameof(T), (int)len, (int)meta.Capacity);

        _driverBuffer.UploadUniformBufferData(uboRef, MemoryMarshal.AsBytes(data), offset, len);
    }

    public void UploadUniformBytes(UniformBufferId uboId, ReadOnlySpan<byte> data, int stride, int length, nint offset)
    {
        //UniformBufferUtils.IsStd140AlignedOrThrow<T>(out nint stride);
        var uboRef = _uboStore.GetRefHandle(uboId);
        _driverBuffer.UploadUniformBufferData(uboRef, MemoryMarshal.AsBytes(data), offset, stride * length);
    }

    public void BindUniformBufferRange(UniformBufferId uboId, nint offset, nint size)
    {
        var uboRef = _uboStore.GetRefAndMeta(uboId, out var meta);
        _driverBuffer.BindUniformBufferRange(uboRef, meta.Slot, offset, size);
    }

    public (nint Offset, nint Size) ToSizeAndOffset<T>(int offsetElements, int count) where T : unmanaged
    {
        var stride = (nint)Unsafe.SizeOf<T>();
        return (offsetElements * stride, count * stride);
    }

    public (int Stride, nint Size) ToStrideAndSize<T>(int count) where T : unmanaged
    {
        var stride = Unsafe.SizeOf<T>();
        return (stride, count * stride);
    }
}