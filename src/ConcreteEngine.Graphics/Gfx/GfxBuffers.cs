using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.OpenGL.Utilities;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxBuffers
{
    private readonly GlBuffers _driverBuffer;

    private readonly VboStore _vboStore;
    private readonly IboStore _iboStore;
    private readonly UboStore _uboStore;
    
    private static long _vboUploadSize;
    private static long _iboUploadSize;
    private static long _uboUploadSize;

    internal GfxBuffers(GfxContextInternal context)
    {
        _driverBuffer = context.Driver.Buffers;
        _vboStore = context.Resources.GfxStoreHub.VboStore;
        _iboStore = context.Resources.GfxStoreHub.IboStore;
        _uboStore = context.Resources.GfxStoreHub.UboStore;
    }

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
        var vboRef = _driverBuffer.CreateVertexBuffer(payload, new CreateBufferInfo(size, storage, access));

        return _vboStore.Add(meta, vboRef);
    }

    public IndexBufferId CreateIndexBuffer<T>(ReadOnlySpan<T> data, BufferStorage storage, BufferAccess access, int length = 0) where T : unmanaged
    {
        var stride = Unsafe.SizeOf<T>();
        var componentCount = data.Length;
        if (componentCount == 0 && length > 0) componentCount = length;
        var size = (uint)stride * (uint)componentCount;
        var usage = storage.ToBufferUsage();

        if (stride != 1 && stride != 2 && stride != 4)
            GraphicsException.ThrowInvalidType(typeof(T).Name, "Invalid elemental size");

        var meta = new IndexBufferMeta(componentCount, stride, usage, storage, access);
        var iboRef = _driverBuffer.CreateIndexBuffer(MemoryMarshal.AsBytes(data),
            new CreateBufferInfo(size, storage, access));

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

        var blockSize = (uint)Unsafe.SizeOf<T>();
        var stride = IntMath.AlignUp(blockSize, UniformBufferUtils.UboOffsetAlign);
        
        var meta = new UniformBufferMeta(slot, (int)stride, stride, 
            BufferUsage.DynamicDraw,
            BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        var uboRef = _driverBuffer.CreateUniformBuffer(slot, new CreateBufferInfo(stride, storage, access));
        return _uboStore.Add(meta, uboRef);
    }


    public void SetVertexBufferData<T>(VertexBufferId vboId, uint offset, ReadOnlySpan<T> data, BufferUsage usage)
        where T : unmanaged
    {
        var vboRef = _vboStore.GetHandleAndMeta(vboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.Stride > 0)
            GraphicsException.ThrowInvalidBufferData(nameof(vboId), "Buffer is static");

        var (stride, size) = ToStrideAndSize<T>(data.Length);
        _driverBuffer.SetVertexBufferData(vboRef, MemoryMarshal.AsBytes(data), size, usage);

        var newMeta = VertexBufferMeta.CreateCopy(in meta, data.Length, stride, offset, usage);
        _vboStore.ReplaceMeta(vboId, in newMeta, out _);
    }

    public void SetIndexBufferData<T>(IndexBufferId iboId, ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged
    {
        var iboRef = _iboStore.GetHandleAndMeta(iboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.Stride > 0)
            GraphicsException.ThrowInvalidBufferData(nameof(iboId), "Buffer is static");

        var (stride, size) = ToStrideAndSize<T>(data.Length);
        _driverBuffer.SetIndexBufferData(iboRef, MemoryMarshal.AsBytes(data), size, usage);

        var newMeta = IndexBufferMeta.CreateCopy(in meta, data.Length, stride, usage);
        _iboStore.ReplaceMeta(iboId, in newMeta, out _);
    }

    public void SetUniformBufferCapacity(UniformBufferId uboId, uint capacity)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(0, (int)capacity);
        var handle = _uboStore.GetHandleAndMeta(uboId, out var meta);
        if (meta.Capacity == capacity) return;
        var newMeta = UniformBufferMeta.MakeResizeCopy(in meta, capacity);
        _uboStore.ReplaceMeta(uboId, in newMeta, out _);
        _driverBuffer.ResizeUniformBuffer(handle, capacity, BufferUsage.DynamicDraw);
    }

    public void ClearUniformBufferData(UniformBufferId uboId)
    {
        var handle = _uboStore.GetHandleAndMeta(uboId, out var meta);
        _driverBuffer.ResizeUniformBuffer(handle, meta.Capacity, BufferUsage.DynamicDraw);
    }

    public void SetVertexBufferCapacity(VertexBufferId vboId, int elements)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(0, elements);
        var handle = _vboStore.GetHandleAndMeta(vboId, out var meta);
        if (meta.ElementCount == elements) return;

        var capacity = (uint)elements * (uint)meta.Stride;
        _driverBuffer.ResizeVertexBuffer(handle, capacity, meta.Usage);
        _vboStore.ReplaceMeta(vboId, meta with { ElementCount = elements }, out _);
    }


    public void UploadVertexBuffer<T>(VertexBufferId vboId, ReadOnlySpan<T> data, int offsetElements)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetElements, data.Length);
        var (offset, size) = ToSizeAndOffset<T>(offsetElements, data.Length);

        var vboRef = _vboStore.GetHandle(vboId);
        var bytes = MemoryMarshal.AsBytes(data);
        _driverBuffer.UploadVertexBufferData(vboRef, bytes, offset, size);
        _vboUploadSize += bytes.Length;
    }

    public void UploadIndexBuffer<T>(IndexBufferId iboId, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetElements, data.Length);
        var iboRef = _iboStore.GetHandle(iboId);
        var (offset, size) = ToSizeAndOffset<T>(offsetElements, data.Length);
        var bytes = MemoryMarshal.AsBytes(data);
        _driverBuffer.UploadIndexBufferData(iboRef, bytes, offset, size);
        _iboUploadSize += bytes.Length;
    }


    public unsafe void UploadSingleUniform<T>(UniformBufferId uboId, T* data, uint offset) where T : unmanaged
    {
        //UniformBufferUtils.IsStd140AlignedOrThrow<T>(out nint stride);
        var uboRef = _uboStore.GetHandle(uboId);
        var size = (uint)Unsafe.SizeOf<T>();
        _driverBuffer.UploadUniformBufferData(uboRef, (byte*)data, offset, size);
        _uboUploadSize += size;
    }
    
    public unsafe void UploadUniform<T>(UniformBufferId uboId, NativeView<T> data, uint offset) where T : unmanaged
    {
        var handle = _uboStore.GetHandleAndMeta(uboId, out var meta);

        var stride = Unsafe.SizeOf<T>();
        var sizeInBytes = (uint)stride * (uint)data.Length;
        
        Debug.Assert(stride == meta.Stride,$"Invalid stride {stride},  expected {meta.Stride}");
        if (offset + sizeInBytes > meta.Capacity)
            GraphicsException.ThrowCapabilityExceeded(nameof(T), (int)sizeInBytes, (int)meta.Capacity);
        
        _driverBuffer.UploadUniformBufferData(handle, (byte*)data.Ptr, offset, sizeInBytes);
        _uboUploadSize += sizeInBytes;
    }
   
    /*
    public unsafe void UploadUniformBytes(UniformBufferId uboId, NativeView<byte> data, int elementCount, int stride, uint offset)
    {
        //UniformBufferUtils.IsStd140AlignedOrThrow<T>(out nint stride);
        var uboRef = _uboStore.GetHandle(uboId);
        _driverBuffer.UploadUniformBufferData(uboRef, data.Ptr, offset, (uint)stride * (uint)elementCount);
        _uboUploadSize += data.Length;
    }
*/
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindUniformBufferRange(UniformBufferId uboId, uint offset, uint size)
    {
        var handle = _uboStore.GetHandleAndMeta(uboId, out var meta);
        _driverBuffer.BindUniformBufferRange(handle, meta.Slot, offset, size);
    }    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindUniformBufferRange(UniformBufferId uboId, UboSlot slot, uint offset, uint size)
    {
        _driverBuffer.BindUniformBufferRange(_uboStore.GetHandle(uboId), slot, offset, size);
    }


    private static (uint Offset, uint Size) ToSizeAndOffset<T>(int offsetElements, int count) where T : unmanaged
    {
        var stride = (uint)Unsafe.SizeOf<T>();
        return ((uint)offsetElements * stride, (uint)count * stride);
    }

    private static (int Stride, uint Size) ToStrideAndSize<T>(int count) where T : unmanaged
    {
        var stride = Unsafe.SizeOf<T>();
        return (stride, (uint)count * (uint)stride);
    }
}