using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxBuffers
{
    private readonly GfxStoreHub _resources;
    private readonly GfxResourceRepository _repository;
    private readonly GfxBuffersBackend _backend;

    private const BufferUsage DefaultUsage = BufferUsage.StaticDraw;

    internal GfxBuffers(GfxContextInternal context)
    {
        _backend = new GfxBuffersBackend(context);
        _resources = context.Stores;
        _repository = context.Repositories;
    }

    //BufferStorage.Dynamic, BufferAccess.MapWrite
    public VertexBufferId CreateVertexBuffer<T>(ReadOnlySpan<T> data, int index, BufferStorage storage, BufferAccess access)
        where T : unmanaged
    {
        (nint stride, nint size) = GfxUtils.ToStrideAndSize<T>(data.Length);
        var meta = new VertexBufferMeta(index, data.Length, stride, DefaultUsage, storage, access);
        var vboRef = _backend.CreateVertexBuffer(data, new GfxBufferDataDesc(size, storage, access));
        return _resources.VboStore.Add(in meta, in vboRef);
    }

    //BufferStorage.Static, BufferAccess.None
    public IndexBufferId CreateIndexBuffer<T>(ReadOnlySpan<T> data, BufferStorage storage, BufferAccess access) where T : unmanaged
    {
        (nint stride, nint size) = GfxUtils.ToStrideAndSize<T>(data.Length);
        if (stride != 1 && stride != 2 && stride != 4)
            GraphicsException.ThrowInvalidType<T>(typeof(T).Name, "Invalid elemental size");

        var meta = new IndexBufferMeta(data.Length, stride, DefaultUsage, storage, access);
        var iboRef = _backend.CreateIndexBuffer(data, new GfxBufferDataDesc(size, storage, access));
        return _resources.IboStore.Add(meta, iboRef);
    }

    //BufferStorage.Dynamic, BufferAccess.MapWrite
    public UniformBufferId CreateUniformBuffer<T>(UniformGpuSlot slot, UboDefaultCapacity defaultCapacity,
        BufferStorage storage = BufferStorage.Dynamic, BufferAccess access = BufferAccess.MapWrite)
        where T : unmanaged, IUniformGpuData
    {
        if (!UniformBufferUtils.IsStd140Aligned<T>())
            throw GraphicsException.InvalidStd140Layout<T>();

        var size = (nint)Unsafe.SizeOf<T>();
        var meta = new UniformBufferMeta(slot, size, BufferUsage.DynamicDraw, BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        var uboRef = _backend.CreateUniformBuffer<T>(slot, new GfxBufferDataDesc(size, storage, access));

        var uboId = _resources.UboStore.Add(in meta, uboRef);
        _repository.ShaderRepository.AddUboToSlot(meta.Slot, uboId);
        return uboId;
    }


    public void SetVertexBufferData<T>(VertexBufferId vboId, ReadOnlySpan<T> data, BufferUsage usage)
        where T : unmanaged
    {
        var vboRef = _resources.VboStore.GetRefAndMeta(vboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.Stride > 0)
            GraphicsException.ThrowInvalidBufferData<VertexBufferId>(nameof(vboId), "Buffer is static");

        (nint stride, nint size) = GfxUtils.ToStrideAndSize<T>(data.Length);
        _backend.SetBufferData(vboRef, data, size, usage);

        var newMeta = VertexBufferMeta.CreateCopy(in meta, data.Length, stride, usage);
        _resources.VboStore.ReplaceMeta(vboId, in newMeta, out _);
    }

    public void SetIndexBufferData<T>(IndexBufferId iboId, ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged
    {
        var iboRef = _resources.IboStore.GetRefAndMeta(iboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.Stride > 0)
            GraphicsException.ThrowInvalidBufferData<IndexBufferId>(nameof(iboId), "Buffer is static");

        (nint stride, nint size) = GfxUtils.ToStrideAndSize<T>(data.Length);
        _backend.SetBufferData(iboRef, data, size, usage);

        var newMeta = IndexBufferMeta.CreateCopy(in meta, data.Length, stride, usage);
        _resources.IboStore.ReplaceMeta(iboId, in newMeta, out _);
    }

    public void SetUniformBufferCapacity(UniformGpuSlot slot, nint capacity)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(0, (int)capacity);
        var ubo = _repository.ShaderRepository.GetUboId(slot);
        var refToken = _resources.UboStore.GetRef(ubo);
        _backend.ResizeBuffer(refToken, capacity, BufferUsage.DynamicDraw);
    }

    public void UploadVertexBuffer<T>(VertexBufferId vboId, ReadOnlySpan<T> data, int offsetElements)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetElements, data.Length);
        var (offset, size) = GfxUtils.ToSizeAndOffset<T>(offsetElements, data.Length);

        var vboRef = _resources.VboStore.GetRef(vboId);
        _backend.UploadBufferData(vboRef, data, offset, size);
    }

    public void UploadIndexBuffer<T>(IndexBufferId iboId, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetElements, data.Length);
        var iboRef = _resources.IboStore.GetRef(iboId);
        var (offset, size) = GfxUtils.ToSizeAndOffset<T>(offsetElements, data.Length);
        _backend.UploadBufferData(iboRef, data, offset, size);
    }


    public void UploadUniformGpuData<T>(UniformGpuSlot slot, in T data, nint offset)
        where T : unmanaged, IUniformGpuData
    {
        UniformBufferUtils.IsStd140AlignedOrThrow<T>(out nint stride);
        var ubo = _repository.ShaderRepository.GetUboId(slot);
        var uboRef = _resources.UboStore.GetRef(ubo);
        _backend.UploadBufferDataSingle(uboRef, data, offset, stride);
    }

    public void BindUniformBufferRange(UniformGpuSlot slot, nint offset, nint size)
    {
        var ubo = _repository.ShaderRepository.GetUboId(slot);
        var uboRef = _resources.UboStore.GetRef(ubo);
        _backend.BindUniformBufferRange(uboRef, (uint)slot, offset, size);
    }


    private sealed class GfxBuffersBackend(GfxContextInternal context)
    {
        private readonly IGraphicsDriver _driver = context.Driver;
        private readonly GlBuffers _driverBuffer = context.Driver.Buffers;

        public GfxRefToken<VertexBufferId> CreateVertexBuffer<T>(ReadOnlySpan<T> data, in GfxBufferDataDesc desc)
            where T : unmanaged
        {
            var vboRef = _driverBuffer.CreateVertexBuffer(data, in desc);
            return vboRef;
        }

        public GfxRefToken<IndexBufferId> CreateIndexBuffer<T>(ReadOnlySpan<T> data, in GfxBufferDataDesc desc)
            where T : unmanaged
        {
            var vboRef = _driverBuffer.CreateIndexBuffer(data, in desc);
            return vboRef;
        }

        public GfxRefToken<UniformBufferId> CreateUniformBuffer<T>(UniformGpuSlot slot, in GfxBufferDataDesc desc)
            where T : unmanaged, IUniformGpuData
        {
            var vboRef = _driverBuffer.CreateUniformBuffer<T>(slot, in desc);
            return vboRef;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBufferData<TId, T>(GfxRefToken<TId> token, ReadOnlySpan<T> data, nint size, BufferUsage usage)
            where TId : unmanaged, IResourceId where T : unmanaged
        {
            _driverBuffer.SetBufferData(token.Handle, data, size, usage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResizeBuffer<TId>(GfxRefToken<TId> token, nint size, BufferUsage usage)
            where TId : unmanaged, IResourceId
        {
            _driverBuffer.ResizeBuffer(token.Handle, size, usage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UploadBufferData<TId, T>(GfxRefToken<TId> token, ReadOnlySpan<T> data, nint offset, nint size)
            where TId : unmanaged, IResourceId where T : unmanaged
        {
            _driverBuffer.UploadBufferData<T>(token.Handle, data, offset, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UploadBufferDataSingle<TId, T>(GfxRefToken<TId> token, T data, nint offset, nint size)
            where TId : unmanaged, IResourceId where T : unmanaged
        {
            _driverBuffer.UploadBufferData<T>(token.Handle, data, offset, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindUniformBufferRange(in GfxRefToken<UniformBufferId> uboRef, uint slot, nint offset, nint size)
        {
            _driverBuffer.BindBufferRange(uboRef.Handle, slot, offset, size);
        }
    }
}