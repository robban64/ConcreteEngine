#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxBuffers
{
    private readonly GlBuffers _driverBuffer;

    private readonly GfxStoreHub _resources;
    private readonly GfxResourceRepository _repository;

    private const BufferUsage DefaultUsage = BufferUsage.StaticDraw;

    internal GfxBuffers(GfxContextInternal context)
    {
        _driverBuffer = context.Driver.Buffers;
        _resources = context.Stores;
        _repository = context.Repositories;
    }

    //BufferStorage.Dynamic, BufferAccess.MapWrite
    public VertexBufferId CreateVertexBuffer<T>(ReadOnlySpan<T> data, int index, BufferStorage storage,
        BufferAccess access)
        where T : unmanaged
    {
        //float -> stride = 4; Vector3 -> stride = 12
        (nint stride, nint size) = ToStrideAndSize<T>(data.Length);
        var meta = new VertexBufferMeta(index, data.Length, stride, DefaultUsage, storage, access);
        var vboRef = _driverBuffer.CreateVertexBuffer(ToBufferByteData(data),
            new GfxBufferDataDesc(size, storage, access));

        return _resources.VboStore.Add(meta, vboRef);
    }

    //BufferStorage.Static, BufferAccess.None
    public IndexBufferId CreateIndexBuffer<T>(ReadOnlySpan<T> data, BufferStorage storage, BufferAccess access)
        where T : unmanaged
    {
        (nint stride, nint size) = ToStrideAndSize<T>(data.Length);
        if (stride != 1 && stride != 2 && stride != 4)
            GraphicsException.ThrowInvalidType<T>(typeof(T).Name, "Invalid elemental size");

        var meta = new IndexBufferMeta(data.Length, stride, DefaultUsage, storage, access);
        var iboRef = _driverBuffer.CreateIndexBuffer(ToBufferByteData(data),
            new GfxBufferDataDesc(size, storage, access));
        return _resources.IboStore.Add(meta, iboRef);
    }

    //BufferStorage.Dynamic, BufferAccess.MapWrite
    public UniformBufferId CreateUniformBuffer<T>(
        UboSlot slot,
        BufferStorage storage = BufferStorage.Dynamic,
        BufferAccess access = BufferAccess.MapWrite) where T : unmanaged, IStd140Uniform
    {
        if (!UniformBufferUtils.IsStd140Aligned<T>())
            throw GraphicsException.InvalidStd140Layout<T>();

        var blockSize = (nint)Unsafe.SizeOf<T>();
        var stride = UniformBufferUtils.AlignUp(blockSize, UniformBufferUtils.UboOffsetAlign);
        var meta = new UniformBufferMeta(slot, stride, stride, BufferUsage.DynamicDraw, BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        var uboRef = _driverBuffer.CreateUniformBuffer(slot, new GfxBufferDataDesc(stride, storage, access));

        var uboId = _resources.UboStore.Add(meta, uboRef);
        return uboId;
    }


    public void SetVertexBufferData<T>(VertexBufferId vboId, ReadOnlySpan<T> data, BufferUsage usage)
        where T : unmanaged
    {
        var vboRef = _resources.VboStore.GetRefAndMeta(vboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.Stride > 0)
            GraphicsException.ThrowInvalidBufferData<VertexBufferId>(nameof(vboId), "Buffer is static");

        (nint stride, nint size) = ToStrideAndSize<T>(data.Length);
        _driverBuffer.SetVertexBufferData(vboRef, ToBufferByteData(data), size, usage);

        var newMeta = VertexBufferMeta.CreateCopy(in meta, data.Length, stride, usage);
        _resources.VboStore.ReplaceMeta(vboId, in newMeta, out _);
    }

    public void SetIndexBufferData<T>(IndexBufferId iboId, ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged
    {
        var iboRef = _resources.IboStore.GetRefAndMeta(iboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.Stride > 0)
            GraphicsException.ThrowInvalidBufferData<IndexBufferId>(nameof(iboId), "Buffer is static");

        (nint stride, nint size) = ToStrideAndSize<T>(data.Length);
        _driverBuffer.SetIndexBufferData(iboRef, ToBufferByteData(data), size, usage);

        var newMeta = IndexBufferMeta.CreateCopy(in meta, data.Length, stride, usage);
        _resources.IboStore.ReplaceMeta(iboId, in newMeta, out _);
    }

    public void SetUniformBufferCapacity(UniformBufferId uboId, nint capacity)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(0, (int)capacity);
        var refToken = _resources.UboStore.GetRefAndMeta(uboId, out var meta);
        if (meta.Capacity == capacity) return;
        var newMeta = UniformBufferMeta.MakeResizeCopy(in meta, capacity);
        _resources.UboStore.ReplaceMeta(uboId, in newMeta, out _);

        _driverBuffer.ResizeUniformBuffer(refToken, capacity, BufferUsage.DynamicDraw);
    }

    public void UploadVertexBuffer<T>(VertexBufferId vboId, ReadOnlySpan<T> data, int offsetElements)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetElements, data.Length);
        var (offset, size) = ToSizeAndOffset<T>(offsetElements, data.Length);

        var vboRef = _resources.VboStore.GetRefHandle(vboId);
        _driverBuffer.UploadVertexBufferData(vboRef, ToBufferByteData(data), offset, size);
    }

    public void UploadIndexBuffer<T>(IndexBufferId iboId, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetElements, data.Length);
        var iboRef = _resources.IboStore.GetRefHandle(iboId);
        var (offset, size) = ToSizeAndOffset<T>(offsetElements, data.Length);
        _driverBuffer.UploadIndexBufferData(iboRef, ToBufferByteData(data), offset, size);
    }


    public void UploadUniformGpuData<T>(UniformBufferId uboId, in T data, nint offset)
        where T : unmanaged, IStd140Uniform
    {
        UniformBufferUtils.IsStd140AlignedOrThrow<T>(out nint stride);
        var uboRef = _resources.UboStore.GetRefHandle(uboId);

        var tSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in data), 1);
        var bytes = MemoryMarshal.AsBytes(tSpan);

        _driverBuffer.UploadUniformBufferData(uboRef, bytes, offset, stride);
    }

    public void BindUniformBufferRange(UniformBufferId uboId, nint offset, nint size)
    {
        var uboRef = _resources.UboStore.GetRefAndMeta(uboId, out var meta);
        _driverBuffer.BindUniformBufferRange(uboRef, meta.Slot, offset, size);
    }

    public static (nint Offset, nint Size) ToSizeAndOffset<T>(int offsetElements, int count) where T : unmanaged
    {
        var stride = (nint)Unsafe.SizeOf<T>();
        return (offsetElements * stride, count * stride);
    }

    public static (nint Stride, nint Size) ToStrideAndSize<T>(int count) where T : unmanaged
    {
        var stride = (nint)Unsafe.SizeOf<T>();
        return (stride, count * stride);
    }

    public static ReadOnlySpan<byte> ToBufferByteData<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        return MemoryMarshal.AsBytes(data);
    }
}