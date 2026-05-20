using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Mesh;

[StructLayout(LayoutKind.Sequential)]
public struct FoliageGpuInstance
{
    public Half4 PositionSize;
    public ColorRgba Color;
}

internal sealed class TerrainChunkMesh(int slot) : IDisposable
{
    private const int Capacity = TerrainChunk.ChunkSamples * TerrainChunk.ChunkSamples;

    public readonly int Slot = slot;
    
    public MeshId TerrainMeshId;
    public VertexBufferId TerrainVboId;

    public MeshId FoliageMeshId;
    public VertexBufferId FoliageInstanceVboId;

    public BoundingBox Bounds;

    private NativeArray<Vertex3D> _vertices = NativeArray.Allocate<Vertex3D>(Capacity, false);
    private NativeView<FoliageGpuInstance> _foliageInstanceData = NativeView<FoliageGpuInstance>.MakeNull();

    public bool HasNullBuffer => _vertices.IsNull;
    public int BufferLength => _vertices.Length;
    public int FoliageCount => _foliageInstanceData.Length;
    
    public NativeView<Vertex3D> GetVertices() => _vertices;
    public NativeView<FoliageGpuInstance> GetFoliageInstances() => _foliageInstanceData;

    public void SetFoliagePtr(NativeView<FoliageGpuInstance> data)
    {
        if(data.IsNull) throw new ArgumentNullException(nameof(data));
        _foliageInstanceData = data;
    }

    public void Dispose()
    {
        _vertices.Dispose();
        _foliageInstanceData = NativeView<FoliageGpuInstance>.MakeNull();
    }
}
