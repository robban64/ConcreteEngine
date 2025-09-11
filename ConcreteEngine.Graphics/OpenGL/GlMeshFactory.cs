using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

public interface IMeshFactory
{
    IMeshLayout CreateArrayMesh<TVertex>(GpuVboDescriptor<TVertex> vertexData, in GpuMeshDescriptor desc)
        where TVertex : unmanaged;

    IMeshLayout CreateElementalMesh<TVertex, TIndex>(GpuVboDescriptor<TVertex> vertexData,
        GpuIboDescriptor<TIndex> indexData, in GpuMeshDescriptor desc)
        where TVertex : unmanaged where TIndex : unmanaged;

    void StartBuilder(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement, uint drawCount = 0);
    IMeshLayout BuildMesh(ReadOnlySpan<VertexAttributeDescriptor> attributes);
    void CreateVertexBuffer<V>(GpuVboDescriptor<V> desc) where V : unmanaged;
    void CreateIndexBuffer<I>(GpuIboDescriptor<I> desc) where I : unmanaged;
}

internal sealed class MeshFactory : IMeshFactory
{
    private readonly IGraphicsDevice _graphics;
    private IGraphicsContext Gfx => _graphics.Gfx;

    private GlResourceStoreView _stores;

    private DrawPrimitive _primitive;
    private MeshDrawKind _drawKind;
    private DrawElementType _elementType;
    private uint _drawCount = 0;
    private uint _calculatedDrawCount = 0;

    private MeshId _meshId = default;
    private IndexBufferId _iboId = default;

    private List<VertexBufferId> _vboIds = new();
    private List<VertexAttributeDescriptor> _attributes = new();


    private bool _isStatic = true;

    internal MeshFactory(IGraphicsDevice graphics, GlResourceStoreView stores)
    {
        _graphics = graphics;
        _stores = stores;
    }

    public IMeshLayout CreateArrayMesh<TVertex>(GpuVboDescriptor<TVertex> vertexData, in GpuMeshDescriptor desc)
        where TVertex : unmanaged
    {
        StartBuilder(desc.Primitive, desc.DrawKind, DrawElementType.Invalid, desc.DrawCount);
        CreateVertexBuffer(vertexData);
        return BuildMesh(desc.Attributes);
    }

    public IMeshLayout CreateElementalMesh<TVertex, TIndex>(GpuVboDescriptor<TVertex> vertexData,
        GpuIboDescriptor<TIndex> indexData, in GpuMeshDescriptor desc)
        where TVertex : unmanaged where TIndex : unmanaged
    {
        StartBuilder(desc.Primitive, desc.DrawKind, desc.ElementType, desc.DrawCount);
        CreateVertexBuffer(vertexData);
        CreateIndexBuffer(indexData);
        return BuildMesh(desc.Attributes);
    }


    public void StartBuilder(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement,
        uint drawCount = 0)
    {
        Debug.Assert(!_meshId.IsValid());

        _primitive = primitive;
        _drawKind = drawKind;
        _elementType = drawElement;
        _drawCount = drawCount;

        _meshId = _graphics.CreateMesh(primitive, drawKind, drawElement);
        Gfx.BindMesh(_meshId);
    }

    public IMeshLayout BuildMesh(ReadOnlySpan<VertexAttributeDescriptor> attributes)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(attributes.Length, 0, nameof(attributes));
        if (!_meshId.IsValid() || _vboIds.Count == 0)
            throw GraphicsException.InvalidState("Missing mesh data");

        if (_drawCount == 0 && _calculatedDrawCount == 0)
            throw GraphicsException.InvalidState("Draw count is 0 for mesh");


        var drawCount = _drawCount > 0 ? _drawCount : _calculatedDrawCount;
        
        var prevMeta = _stores.MeshStore.GetMeta(_meshId);
        var newMeta = new MeshMeta(prevMeta.Primitive,prevMeta.DrawKind,prevMeta.ElementType,prevMeta.VertexAttribPointers, drawCount);
        _stores.MeshStore.ReplaceMeta(_meshId, in newMeta, out _);

        var meshRegistry = _stores.MeshRegistry;
        meshRegistry.UpdateVboIds(_meshId, _vboIds);

        if (_iboId.IsValid())
            meshRegistry.UpdateIboId(_meshId, _iboId);

        meshRegistry.UpdateAttributes(_meshId, attributes);
        var result = meshRegistry.Get(_meshId);

        // Most come after meshRegistry
        Gfx.SetVertexAttribute(attributes);
        Gfx.BindMesh(default);
        Gfx.BindVertexBuffer(default);
        Gfx.BindIndexBuffer(default);

        _iboId = default;
        _meshId = default;
        _isStatic = true;
        _drawCount = 0;
        _calculatedDrawCount = 0;
        _drawKind = MeshDrawKind.Arrays;
        _primitive = DrawPrimitive.Triangles;
        _elementType = DrawElementType.Invalid;
        _vboIds.Clear();
        _attributes.Clear();
        return result;
    }


    public void CreateVertexBuffer<V>(GpuVboDescriptor<V> desc) where V : unmanaged
    {
        _meshId.IsValidOrThrow();
        ArgumentOutOfRangeException.ThrowIfNotEqual(desc.BindingIndex, (uint)_vboIds.Count);

        if (desc.Usage != BufferUsage.StaticDraw) _isStatic = false;

        if (!_iboId.IsValid() && _vboIds.Count == 0) _calculatedDrawCount = (uint)desc.Data.Length;

        uint size = (uint)Unsafe.SizeOf<V>();
        var vboId = _graphics.CreateVertexBuffer(desc.Usage, size, desc.BindingIndex);
        Gfx.BindVertexBuffer(vboId);
        Gfx.SetVertexBuffer(desc.Data, desc.Usage);

        _vboIds.Add(vboId);
    }

    public void CreateIndexBuffer<I>(GpuIboDescriptor<I> desc) where I : unmanaged
    {
        _meshId.IsValidOrThrow();
        if (_iboId.IsValid())
            throw GraphicsException.InvalidState("IboId is bound from previous state");


        if (desc.Usage != BufferUsage.StaticDraw) _isStatic = false;
        _calculatedDrawCount = (uint)desc.Data.Length;

        var size = (uint)Unsafe.SizeOf<I>();
        if (size < 1 || size > 4)
            GraphicsException.ThrowInvalidType<I>(typeof(I).Name, "Invalid Size");

        var iboId = _graphics.CreateIndexBuffer(desc.Usage, size);
        Gfx.BindIndexBuffer(iboId);
        Gfx.SetIndexBuffer(desc.Data, desc.Usage);
    }
}

/*
internal sealed class GlMeshFactory : IMeshFactory
{
    private readonly record struct IboResult(
        GlIndexBufferHandle Handle,
        DrawElementType ElementType,
        in IndexBufferMeta Meta);

    private readonly GlGraphicsDevice _graphics;
    private readonly MeshRegistry _meshRegistry;
    private GlGraphicsContext Gfx => _graphics.Gfx;
    private GL Gl => _graphics.Gl;

    private GlResourceStoreView _stores;


    private DrawPrimitive _primitive;
    private MeshDrawKind _drawKind;
    private DrawElementType _elementType;
    private uint _drawCount = 0;
    private uint _calculatedDrawCount = 0;

    private GlMeshHandle? _vaoHandle = null;
    private List<(VertexBufferMeta ,GlVertexBufferHandle)> _vboResult = new();
    private List<VertexAttributeDescriptor> _attributes = new();

    private IboResult? _iboResult = null;

    private bool _isStatic = true;

    public GlMeshFactory(GlGraphicsDevice graphics, GlResourceStoreView stores, MeshRegistry meshRegistry)
    {
        _graphics = graphics;
        _meshRegistry = meshRegistry;
        _stores = stores;
    }

    public void StartBuilder(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement, uint drawCount = 0)
    {
        Debug.Assert(_vaoHandle == null);

        _primitive = primitive;
        _drawKind = drawKind;
        _elementType = drawElement;
        _drawCount = drawCount;

        var handle = CreateVao();
        Gl.BindVertexArray(handle.Handle);
        _vaoHandle = handle;
    }

    public IMeshLayout BuildMesh(ReadOnlySpan<VertexAttributeDescriptor> attributes)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(attributes.Length, 0, nameof(attributes));
        if (_vaoHandle is null || _vboResult.Count == 0)
            throw GraphicsException.InvalidState("Missing mesh data");

        for (int i = 0; i < attributes.Length; i++)
        {
            ref readonly var attrib = ref attributes[i];
            AddAttribPointer((uint)i, (int)attrib.Format, attrib.StrideBytes, attrib.OffsetBytes, attrib.Normalized);

        }

        Gl.BindVertexArray(0);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);


        var elementType = _iboResult?.ElementType ?? DrawElementType.Invalid;

        var meshStore = _stores.MeshStore;
        var iboStore = _stores.IboStore;
        var vboStore = _stores.VboStore;


        IndexBufferId iboId = default;
        if (_iboResult is { } ibo)
        {
            iboId = iboStore.Add(ibo.Meta, ibo.Handle);
        }

        var vboIds = new VertexBufferId[_vboResult.Count];
        for (int i = 0; i < _vboResult.Count; i++)
        {
            var (meta, handle) = _vboResult[i];
            vboIds[i] = vboStore.Add(meta, handle);
        }

        var meshMeta = new MeshMeta(_primitive, _drawKind, elementType, (uint)attributes.Length, _drawCount);
        var meshId = meshStore.Add(in meshMeta, _vaoHandle.Value);

        var meshLayout = new MeshLayout
        {
            MeshId = meshId, IndexBufferId = iboId, VertexBufferIds = vboIds, Attributes = attributes.ToArray(),
        };

        _meshRegistry.RegisterMesh(meshLayout);

        _iboResult = null;
        _vaoHandle = null;
        _isStatic = true;
        _drawCount = 0;
        _calculatedDrawCount = 0;
        _drawKind = MeshDrawKind.Arrays;
        _primitive = DrawPrimitive.Triangles;
        _elementType = DrawElementType.Invalid;
        _vboResult.Clear();
        _attributes.Clear();
        return meshLayout;
    }


    public void CreateVertexBuffer<V>(GpuVboDescriptor<V> desc) where V : unmanaged
    {
        ArgumentNullException.ThrowIfNull(_vaoHandle);
        if (desc.Usage != BufferUsage.StaticDraw) _isStatic = false;
        if (_iboResult is null && _vboResult.Count == 0) _calculatedDrawCount = (uint)desc.Data.Length;

        var handle = CreateVertexBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, handle.Handle);
        SetBufferData<V>(BufferTargetARB.ArrayBuffer, desc.Usage.ToGlEnum(), desc.Data);
        uint size = (uint)Unsafe.SizeOf<V>();
        var meta = new VertexBufferMeta(desc.Usage,(uint)_vboResult.Count, (uint)desc.Data.Length, size);
        _vboResult.Add((meta, handle));
    }

    public void CreateIndexBuffer<I>(GpuIboDescriptor<I> desc) where I : unmanaged
    {
        ArgumentNullException.ThrowIfNull(_vaoHandle);
        Debug.Assert(_iboResult == null);

        if (desc.Usage != BufferUsage.StaticDraw) _isStatic = false;
        _calculatedDrawCount = (uint)desc.Data.Length;

        var size = (uint)Unsafe.SizeOf<I>();
        var elementType = size switch
        {
            1 => DrawElementType.UnsignedByte,
            2 => DrawElementType.UnsignedShort,
            4 => DrawElementType.UnsignedInt,
            _ => throw GraphicsException.UnsupportedFeature($"Index Element Size {size}")
        };


        var handle = CreateIndexBuffer();
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, handle.Handle);
        SetBufferData<I>(BufferTargetARB.ElementArrayBuffer, desc.Usage.ToGlEnum(), desc.Data);
        var meta = new IndexBufferMeta(desc.Usage, (uint)desc.Data.Length, size);
        _iboResult = new IboResult(handle, elementType, in meta);
    }

    public GlMeshHandle CreateVao()
    {
        var handle = Gl.GenVertexArray();
        return new GlMeshHandle(handle);
    }

    public GlVertexBufferHandle CreateVertexBuffer()
    {
        var handle = Gl.GenBuffer();
        return new GlVertexBufferHandle(handle);
    }

    public GlIndexBufferHandle CreateIndexBuffer()
    {
        var handle = Gl.GenBuffer();
        return new GlIndexBufferHandle(handle);
    }

    private void SetBufferData<TData>(BufferTargetARB target, BufferUsageARB usage, ReadOnlySpan<TData> data,
        int? dataLength = null)
        where TData : unmanaged
    {
        var elementSize = Unsafe.SizeOf<TData>();
        var length = dataLength.GetValueOrDefault(data.Length);
        var size = length * elementSize;
        Gl.BufferData(target, (nuint)size, data, usage);
    }

    private unsafe void AddAttribPointer(uint index, int size, uint strideBytes, uint offsetBytes,
        bool normalized = false)
    {
        Gl.EnableVertexAttribArray(index);
        Gl.VertexAttribPointer(
            index,
            size,
            VertexAttribPointerType.Float,
            normalized,
            strideBytes,
            (void*)offsetBytes
        );
    }
}
*/