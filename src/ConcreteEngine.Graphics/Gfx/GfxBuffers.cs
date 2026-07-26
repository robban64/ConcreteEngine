using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxBuffers
{
    private static long _vboUploadSize;
    private static long _iboUploadSize;
    private static long _uboUploadSize;

    internal GfxBuffers() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EndFrame(out GpuBufferMeta result)
    {
        result = new GpuBufferMeta(_vboUploadSize + _iboUploadSize, _uboUploadSize);

        _vboUploadSize = 0;
        _iboUploadSize = 0;
        _uboUploadSize = 0;
    }

    //BufferStorage.Dynamic, BufferAccess.MapWrite
    public VertexBufferId CreateVertexBuffer<T>(ReadOnlySpan<T> data, byte divisor, uint offset, BufferStorage storage,
        BufferAccess access, int length = 0) where T : unmanaged
    {
        var stride = Unsafe.SizeOf<T>();
        var componentCount = data.Length;
        if (componentCount == 0 && length > 0) componentCount = length;
        var size = (uint)stride * (uint)componentCount;
        var usage = storage.ToBufferUsage();
        var meta = new VertexBufferMeta(stride, componentCount, offset, divisor, usage, storage, access);

        var payload = data.Length > 0 ? MemoryMarshal.AsBytes(data) : ReadOnlySpan<byte>.Empty;
        var vboHandle = GlBuffers.CreateVertexBuffer(payload, new CreateBufferInfo(size, storage, access));

        return GfxRegistry.VboStore.Add(meta, vboHandle);
    }

    public IndexBufferId CreateIndexBuffer<T>(ReadOnlySpan<T> data, BufferStorage storage, BufferAccess access,
        int length = 0) where T : unmanaged
    {
        var stride = Unsafe.SizeOf<T>();
        var componentCount = data.Length;
        if (componentCount == 0 && length > 0) componentCount = length;
        var size = (uint)stride * (uint)componentCount;
        var usage = storage.ToBufferUsage();

        if (stride != 1 && stride != 2 && stride != 4)
            GraphicsException.ThrowInvalidType(typeof(T).Name, "Invalid elemental size");

        var meta = new IndexBufferMeta(componentCount, stride, usage, storage, access);
        var iboHandle = GlBuffers.CreateIndexBuffer(MemoryMarshal.AsBytes(data),
            new CreateBufferInfo(size, storage, access));

        return GfxRegistry.IboStore.Add(meta, iboHandle);
    }


    public void SetVertexBufferData<T>(VertexBufferId vboId, uint offset, ReadOnlySpan<T> data, BufferUsage usage)
        where T : unmanaged
    {
        var vboHandle = GfxRegistry.VboStore.GetHandleAndMeta(vboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.Stride > 0)
            GraphicsException.ThrowInvalidBufferData(nameof(vboId), "Buffer is static");

        var (stride, size) = ToStrideAndSize<T>(data.Length);
        GlBuffers.SetBufferData(vboHandle, MemoryMarshal.AsBytes(data), size, usage);

        var newMeta = VertexBufferMeta.CreateCopy(in meta, data.Length, stride, offset, usage);
        GfxRegistry.VboStore.ReplaceMeta(vboId, in newMeta, out _);
    }

    public void SetIndexBufferData<T>(IndexBufferId iboId, ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged
    {
        var iboHandle = GfxRegistry.IboStore.GetHandleAndMeta(iboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.Stride > 0)
            GraphicsException.ThrowInvalidBufferData(nameof(iboId), "Buffer is static");

        var (stride, size) = ToStrideAndSize<T>(data.Length);
        GlBuffers.SetBufferData(iboHandle, MemoryMarshal.AsBytes(data), size, usage);

        var newMeta = IndexBufferMeta.CreateCopy(in meta, data.Length, stride, usage);
        GfxRegistry.IboStore.ReplaceMeta(iboId, in newMeta, out _);
    }
    

    public void SetVertexBufferCapacity(VertexBufferId vboId, int elements)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(0, elements);
        var handle = GfxRegistry.VboStore.GetHandleAndMeta(vboId, out var meta);
        if (meta.ElementCount == elements) return;

        var capacity = elements * meta.Stride;
        GlBuffers.ResizeBuffer(handle, capacity, meta.Usage);
        GfxRegistry.VboStore.ReplaceMeta(vboId, meta with { ElementCount = elements }, out _);
    }


    public void UploadVertexBuffer<T>(VertexBufferId vboId, ReadOnlySpan<T> data, int offsetElements)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetElements, data.Length);
        var (offset, size) = ToSizeAndOffset<T>(offsetElements, data.Length);

        var vboHandle = GfxRegistry.VboStore.GetHandle(vboId);
        var bytes = MemoryMarshal.AsBytes(data);
        GlBuffers.UploadBufferData(vboHandle, bytes, offset, size);
        _vboUploadSize += bytes.Length;
    }

    public void UploadIndexBuffer<T>(IndexBufferId iboId, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetElements, data.Length);
        var iboHandle = GfxRegistry.IboStore.GetHandle(iboId);
        var (offset, size) = ToSizeAndOffset<T>(offsetElements, data.Length);
        var bytes = MemoryMarshal.AsBytes(data);
        GlBuffers.UploadBufferData(iboHandle, bytes, offset, size);
        _iboUploadSize += bytes.Length;
    }

    //BufferStorage.Dynamic, BufferAccess.MapWrite
    public UniformBufferId CreateUniformBuffer<T>(
        BufferStorage storage = BufferStorage.Dynamic,
        BufferAccess access = BufferAccess.MapWrite) where T : unmanaged, IUniform
    {
        var stride = T.Stride > 0 ? T.Stride : Unsafe.SizeOf<T>();

        if (!UniformBufferUtils.IsStd140Aligned(stride))
            throw GraphicsException.InvalidStd140Layout(stride);

        stride = IntMath.AlignUp(stride, UniformBufferUtils.UboOffsetAlign);

        var meta = new UniformBufferMeta(T.Slot, stride, stride,
            BufferUsage.DynamicDraw,
            BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        var uboHandle = GlBuffers.CreateUniformBuffer(T.Slot, new CreateBufferInfo((uint)stride, storage, access));
        return GfxRegistry.UboStore.Add(meta, uboHandle);
    }
    
    public void SetUniformBufferCount(UniformBufferId uboId, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(uboId.Id);

        var handle = GfxRegistry.UboStore.GetHandleAndMeta(uboId, out var meta);

        var capacity = meta.Stride * count;
        if (capacity == meta.Capacity) return;
        capacity = IntMath.AlignUp(capacity, meta.Stride);

        var newMeta = UniformBufferMeta.MakeResizeCopy(in meta, capacity);
        GfxRegistry.UboStore.ReplaceMeta(uboId, in newMeta, out _);
        GlBuffers.ResizeBuffer(handle, capacity, BufferUsage.DynamicDraw);
    }
    public void ClearUniformBufferData(UniformBufferId uboId)
    {
        var handle = GfxRegistry.UboStore.GetHandleAndMeta(uboId, out var meta);
        GlBuffers.ResizeBuffer(handle, meta.Capacity, BufferUsage.DynamicDraw);
    }

    public unsafe void UploadSingleUniform<T>(T* data, int offset) where T : unmanaged, IUniform
    {
        var uboHandle = GfxRegistry.UboStore.GetHandle(T.UboId);
        GlBuffers.UploadBufferData(uboHandle, (byte*)data, offset, T.Stride);
        _uboUploadSize += T.Stride;
    }

    public unsafe void UploadUniform<T>(NativeView<T> data, int offset) where T : unmanaged, IUniform
    {
        var handle = GfxRegistry.UboStore.GetHandle(T.UboId);
        var capacity = GfxRegistry.UboStore.GetMeta(T.UboId).Capacity;
        var sizeInBytes = data.Length * T.Stride;

        if (offset + sizeInBytes > capacity)
            GraphicsException.ThrowCapabilityExceeded(nameof(T), sizeInBytes, capacity);

        GlBuffers.UploadBufferData(handle, (byte*)data.Ptr, offset, sizeInBytes);
        _uboUploadSize += sizeInBytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindUniformBufferRange<T>(int offset, int length) where T : unmanaged, IUniform
    {
        var id = GfxRegistry.UboStore.GetHandle(T.UboId);
        GlBuffers.BindUniformBufferRange(id, T.Slot, offset * T.Stride, length * T.Stride);
    }

    private static (uint Offset, uint Size) ToSizeAndOffset<T>(int offsetElements, int count) where T : unmanaged
    {
        var stride = (uint)Unsafe.SizeOf<T>();
        return ((uint)offsetElements * stride, (uint)count * stride);
    }

    private static (int Stride, int Size) ToStrideAndSize<T>(int count) where T : unmanaged
    {
        var stride = Unsafe.SizeOf<T>();
        return (stride, count * stride);
    }
}