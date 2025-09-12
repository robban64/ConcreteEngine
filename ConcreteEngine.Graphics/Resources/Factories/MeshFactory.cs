using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

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
    private readonly IGraphicsContext _gfx;
    private readonly IResourceManager _resources;
    private readonly IResourceAllocator _allocator;
    private readonly IResourceRegistry _registry;

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

    internal MeshFactory(IGraphicsContext gfx, IResourceManager resources, IResourceAllocator allocator,
        IResourceRegistry registry)
    {
        _gfx = gfx;
        _resources = resources;
        _allocator = allocator;
        _registry = registry;
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

        _meshId = _allocator.CreateMesh(primitive, drawKind, drawElement, out _);
        _gfx.BindMesh(_meshId);
    }

    public IMeshLayout BuildMesh(ReadOnlySpan<VertexAttributeDescriptor> attributes)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(attributes.Length, 0, nameof(attributes));
        if (!_meshId.IsValid() || _vboIds.Count == 0)
            throw GraphicsException.InvalidState("Missing mesh data");

        if (_drawCount == 0 && _calculatedDrawCount == 0)
            throw GraphicsException.InvalidState("Draw count is 0 for mesh");


        var drawCount = _drawCount > 0 ? _drawCount : _calculatedDrawCount;

        var prevMeta = _resources.MeshStore.GetMeta(_meshId);
        var newMeta = new MeshMeta(prevMeta.Primitive, prevMeta.DrawKind, prevMeta.ElementType,
            prevMeta.VertexAttribPointers, drawCount);
        _resources.MeshStore.ReplaceMeta(_meshId, in newMeta, out _);

        var meshRegistry = _registry.MeshRegistry;
        meshRegistry.UpdateVboIds(_meshId, _vboIds);

        if (_iboId.IsValid())
            meshRegistry.UpdateIboId(_meshId, _iboId);

        meshRegistry.UpdateAttributes(_meshId, attributes);
        var result = meshRegistry.Get(_meshId);

        // Most come after meshRegistry
        _gfx.SetVertexAttribute(attributes);
        _gfx.BindMesh(default);
        _gfx.BindVertexBuffer(default);
        _gfx.BindIndexBuffer(default);

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
        var vboId = _allocator.CreateVertexBuffer(desc.Usage, size, desc.BindingIndex, out _);
        _gfx.BindVertexBuffer(vboId);
        _gfx.SetVertexBuffer(desc.Data, desc.Usage);

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

        var iboId = _allocator.CreateIndexBuffer(desc.Usage, size, out _);
        _gfx.BindIndexBuffer(iboId);
        _gfx.SetIndexBuffer(desc.Data, desc.Usage);
    }
}