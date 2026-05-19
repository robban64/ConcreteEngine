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

/*
    //Foliage
    GL_TEXTURE_LOD_BIAS, -0.5f
    GL_TEXTURE_MAX_ANISOTROPY_EXT, 8.0f
    GL_TEXTURE_MIN_FILTER = GL_LINEAR_MIPMAP_LINEAR
    GL_TEXTURE_MAG_FILTER = GL_LINEAR
   
    glDisable(GL_BLEND);
   glDisable(GL_CULL_FACE);
   glEnable(GL_SAMPLE_ALPHA_TO_COVERAGE);
   glEnable(GL_DEPTH_TEST);
   glDepthFunc(GL_LEQUAL);
   glDepthMask(GL_TRUE);
   
   
   // Particle
   GL_TEXTURE_LOD_BIAS, +0.5f
   GL_TEXTURE_MAX_ANISOTROPY_EXT, 1.0f
   GL_TEXTURE_MIN_FILTER = GL_LINEAR_MIPMAP_LINEAR / GL_LINEAR_MIPMAP_NEAREST
   GL_TEXTURE_MAG_FILTER = GL_LINEAR

glDisable(GL_CULL_FACE);
glEnable(GL_DEPTH_TEST);
glDepthMask(GL_FALSE);
DepthFunc: GL_LEQUAL

 */
[StructLayout(LayoutKind.Sequential)]
public struct FoliageGpuInstance
{
    public Vector4 PositionSize;
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

    private NativeArray<Vertex3D> _vertices = NativeArray.Allocate<Vertex3D>(Capacity, zeroed: true);
    private NativeArray<FoliageGpuInstance> _foliageInstanceData = NativeArray<FoliageGpuInstance>.MakeNull();

    public bool HasNullBuffer => _vertices.IsNull;
    public int BufferLength => _vertices.Length;
    public int FoliageCount => _foliageInstanceData.Length;
    public NativeView<Vertex3D> GetVertices() => _vertices;
    public NativeView<FoliageGpuInstance> GetFoliageInstances() => _foliageInstanceData;

    public NativeView<FoliageGpuInstance> AllocateOrResizeFoliage(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        //Console.WriteLine("active count: " + count + " : " + IntMath.AlignUp(count, CapacityUtils.PageSize));

        count = IntMath.AlignUp(count, CapacityUtils.PageSize);
        if (_foliageInstanceData.IsNull)
            _foliageInstanceData = NativeArray.Allocate<FoliageGpuInstance>(count, zeroed: true);
        else if (count > _foliageInstanceData.Length)
            _foliageInstanceData.Resize(count, true);

        return _foliageInstanceData;
    }

    public void Dispose()
    {
        _vertices.Dispose();
        _foliageInstanceData.Dispose();
    }
}
