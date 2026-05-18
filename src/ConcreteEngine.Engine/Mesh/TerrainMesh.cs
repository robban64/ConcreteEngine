using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Engine.Mesh;



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

    public VertexBufferId FoliageInstanceVboId;
    //public MeshHandle TerrainMesh;
    //public MeshHandle FoliageMesh;

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

internal sealed class TerrainMesh(GfxContext gfx) : IDisposable
{
    private const int Step = 1;
    private const int IndicesPerQuad = 6;
    private const int ChunkQuads = TerrainChunk.ChunkQuads; // 64
    private const int ChunkSamples = TerrainChunk.ChunkSamples; // 65
    private const int IndexCount = ChunkQuads * ChunkQuads * IndicesPerQuad;

    public const int ChunkCount = 4 * 4;

    public MeshId FoliageMeshId { get; private set; }
    public IndexBufferId TerrainIboId { get; private set; }

    private NativeArray<ushort> _terrainIndexBuffer;
    
    private TerrainChunkMesh[] _meshChunks = [];


    internal ReadOnlySpan<TerrainChunkMesh> GetMeshChunks() => _meshChunks;

    public void Dispose()
    {
        _terrainIndexBuffer.Dispose();
        foreach (var it in _meshChunks)
            it.Dispose();
    }

    public void Allocate(ReadOnlySpan<TerrainChunk> chunks, ReadOnlySpan<byte> data, int dimension, int gridSize,
        float maxHeight)
    {
        if (TerrainIboId.IsValid()) throw new InvalidOperationException("Already allocated");

        _terrainIndexBuffer = NativeArray.Allocate<ushort>(IndexCount);
        FillIndexBuffer(_terrainIndexBuffer);
        var iboArgs = CreateIboArgs.MakeDefault();
        TerrainIboId = gfx.Buffers.CreateIndexBuffer(_terrainIndexBuffer.AsSpan(), iboArgs.Storage, iboArgs.Access, iboArgs.Length);


        int idx = 0;
        _meshChunks = new TerrainChunkMesh[4 * 4];
        foreach (var it in chunks)
        {
            var meshChunk = _meshChunks[idx] = new TerrainChunkMesh(idx);
            //FillVertices(it, meshChunk, dimension);
            //GenerateNormals(it, meshChunk, data, dimension, maxHeight);
            //CalculateBounds(it, meshChunk);
            GenerateCompleteVerticesAndBounds(it, meshChunk, data, dimension, maxHeight);
            CreateChunkMesh(meshChunk);
            idx++;
        }
    }

    private void CreateChunkMesh(TerrainChunkMesh chunkMesh)
    {
        if (chunkMesh.HasNullBuffer) throw new ArgumentNullException(nameof(chunkMesh));

        var args = CreateVboArgs.MakeDynamic(0);
        var props = MeshDrawProperties.MakeElemental(size: DrawElementSize.UnsignedShort, drawCount: IndexCount);

        var vertices = chunkMesh.GetVertices().AsReadOnlySpan();

        var meshId = gfx.Meshes.CreateEmptyMesh(in props, 1, VertexAttributes.GetVertex3DAttributes());
        var vboId = gfx.Meshes.CreateAttachVertexBuffer(meshId, vertices, args);
        gfx.Meshes.AttachIndexBuffer(meshId, TerrainIboId);

        chunkMesh.TerrainMeshId = meshId;
        chunkMesh.TerrainVboId = vboId;
    }

    private static void CalculateBounds(TerrainChunk chunk, TerrainChunkMesh mesh)
    {
        if (mesh.HasNullBuffer) throw new ArgumentNullException(nameof(mesh));
        ArgumentOutOfRangeException.ThrowIfLessThan(mesh.BufferLength, ChunkSamples * ChunkSamples);
        var start = chunk.WorldStart;
        var end = chunk.WorldStart + ChunkQuads;

        float minY = float.MaxValue, maxY = float.MinValue;
        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                float y = chunk.GetHeight(x, z);
                minY = float.Min(minY, y);
                maxY = float.Max(maxY, y);
            }
        }

        InvalidOpThrower.ThrowIf(minY > maxY);
        mesh.Bounds = new BoundingBox(new Vector3(start.X, minY, start.Y), new Vector3(end.X, maxY, end.Y));
    }

    private static void FillIndexBuffer(NativeArray<ushort> indices)
    {
        if (indices.IsNull) throw new ArgumentNullException(nameof(indices));

        ArgumentOutOfRangeException.ThrowIfLessThan(indices.Length, IndexCount, nameof(indices));
        int i = 0;
        for (int z = 0; z < ChunkQuads; z++)
        {
            for (int x = 0; x < ChunkQuads; x++)
            {
                int bottomLeft = z * ChunkSamples + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * ChunkSamples + x;
                int topRight = topLeft + 1;

                indices[i++] = (ushort)bottomLeft;
                indices[i++] = (ushort)topLeft;
                indices[i++] = (ushort)bottomRight;

                indices[i++] = (ushort)topLeft;
                indices[i++] = (ushort)topRight;
                indices[i++] = (ushort)bottomRight;
            }
        }
    }

    private static void FillVertices(TerrainChunk chunk, TerrainChunkMesh mesh, int dimension)
    {
        if (mesh.HasNullBuffer) throw new ArgumentNullException(nameof(mesh));

        var vertices = mesh.GetVertices();
        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                float worldX = chunk.WorldStart.X + x;
                float worldZ = chunk.WorldStart.Y + z;

                float y = chunk.GetHeight(x, z);

                float u = worldX / (dimension - 1);
                float v = worldZ / (dimension - 1);

                int vi = z * ChunkSamples + x;
                ref var vertex = ref vertices[vi];

                vertex.Position = new Vector3(worldX, y, worldZ);
                vertex.TexCoords = new Vector2(u, v);
            }
        }
    }

    private static void GenerateNormals(TerrainChunk chunk, TerrainChunkMesh mesh, ReadOnlySpan<byte> data,
        int dimension, float maxHeight)
    {
        if (mesh.HasNullBuffer) throw new ArgumentNullException(nameof(mesh));

        var vertices = mesh.GetVertices();

        var start = chunk.WorldStart;
        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                var wCoords = start + x;
                var vi = z * ChunkSamples + x;

                ref var v = ref vertices[vi];
                v.Normal = TerrainUtils.GetNormal(data, wCoords.X, wCoords.Y, 1, dimension, maxHeight);
                v.Tangent = TerrainUtils.GetTangent(data, wCoords.X, wCoords.Y, 1, dimension, maxHeight, v.Normal);
            }
        }
    }

    //FillVertices, GenerateNormals, CalculateBounds
    private static void GenerateCompleteVerticesAndBounds(TerrainChunk chunk, TerrainChunkMesh mesh,
        ReadOnlySpan<byte> data,
        int dimension, float maxHeight)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(mesh);
        if (mesh.HasNullBuffer) throw new ArgumentNullException(nameof(mesh));

        var vertices = mesh.GetVertices();

        var start = chunk.WorldStart;
        var end = chunk.WorldStart + ChunkQuads;

        float minY = float.MaxValue, maxY = float.MinValue;
        for (int z = 0; z < ChunkSamples; z++)
        {
            for (int x = 0; x < ChunkSamples; x++)
            {
                float worldX = start.X + x;
                float worldZ = start.Y + z;

                float y = chunk.GetHeight(x, z);
                minY = float.Min(minY, y);
                maxY = float.Max(maxY, y);

                float u = worldX / (dimension - 1);
                float v = worldZ / (dimension - 1);

                int vi = z * ChunkSamples + x;

                ref var vx = ref vertices[vi];

                vx.Position = new Vector3(worldX, y, worldZ);
                vx.TexCoords = new Vector2(u, v);

                vx.Normal = TerrainUtils.GetNormal(data, (int)worldX, (int)worldZ, 1, dimension, maxHeight);
                vx.Tangent =
                    TerrainUtils.GetTangent(data, (int)worldX, (int)worldZ, 1, dimension, maxHeight, vx.Normal);
            }
        }

        InvalidOpThrower.ThrowIf(minY > maxY);
        mesh.Bounds = new BoundingBox(new Vector3(start.X, minY, start.Y), new Vector3(end.X, maxY, end.Y));
    }
    
    private void GenerateFoliageBuffer(TerrainChunk chunk, TerrainChunkMesh mesh)
    {
        const float density = 2.0f; 
        const float step = 1.0f / density;
        const int maxInstanceCount = (int)(ChunkQuads * density * ChunkQuads * density);

        var random = new FastRandom((uint)chunk.WorldStart.GetHashCode());

        var instanceCount = 0;
        for (float z = 0; z < ChunkQuads; z += step)
        {
            for (float x = 0; x < ChunkQuads; x += step)
            {
                if (z % 5 == 0) instanceCount++;
            }
        }
        
        var instanceData = mesh.AllocateOrResizeFoliage(instanceCount);

        for (float z = 0; z < ChunkQuads; z += step)
        {
            for (float x = 0; x < ChunkQuads; x += step)
            {
                // 1. Add Random Jitter
                float offsetX = random.NextFloat() * step;
                float offsetZ = random.NextFloat() * step;
            
                float worldX = chunk.WorldStart.X + x + offsetX;
                float worldZ = chunk.WorldStart.Y + z + offsetZ;

                if (z % 5 != 0) continue;

                float y = chunk.GetHeight((int)x, (int)z);
                
                int vi = (int)(z * ChunkSamples + x);

                ref var instance = ref instanceData[vi];
                float size = random.RandomFloat(0.8f, 1.2f);
                instance.PositionSize = new Vector4(worldX, y, worldZ, size);
                instance.Color = ColorRgba.White;

            }
        }
    }
    
    private unsafe void GenerateFoliageMesh(int instanceCount)
    {
        var normal = Vector3.UnitY;
        var tangent = Vector3.UnitX; 

        ReadOnlySpan<Vertex3D> vertices = stackalloc Vertex3D[]
        {
            new Vertex3D(new Vector3(-0.5f, 0.0f, -0.5f), new Vector2(0f, 0f), normal, tangent),
            new Vertex3D(new Vector3( 0.5f, 0.0f,  0.5f), new Vector2(1f, 0f), normal, tangent),
            new Vertex3D(new Vector3(-0.5f, 1.0f, -0.5f), new Vector2(0f, 1f), normal, tangent),
            new Vertex3D(new Vector3( 0.5f, 1.0f,  0.5f), new Vector2(1f, 1f), normal, tangent),

            new Vertex3D(new Vector3(-0.5f, 0.0f,  0.5f), new Vector2(0f, 0f), normal, tangent),
            new Vertex3D(new Vector3( 0.5f, 0.0f, -0.5f), new Vector2(1f, 0f), normal, tangent),
            new Vertex3D(new Vector3(-0.5f, 1.0f,  0.5f), new Vector2(0f, 1f), normal, tangent),
            new Vertex3D(new Vector3( 0.5f, 1.0f, -0.5f), new Vector2(1f, 1f), normal, tangent) 
        };
        
        ReadOnlySpan<ushort> indices = stackalloc ushort[]
        {
            // Quad 1
            0, 2, 1,
            2, 3, 1,
            // Quad 2
            4, 6, 5,
            6, 7, 5
        };
        
        Span<VertexAttributeDef> attribs = stackalloc VertexAttributeDef[6];
        VertexAttributes.GetVertex3DAttributes().CopyTo(attribs);
        
        var instanceAttributeMaker = new VertexAttributeMaker();
        attribs[4] = instanceAttributeMaker.Make<Vector4>(2, 1);
        attribs[5] = instanceAttributeMaker.Make<ColorRgba>(3, 1, VertexFormat.UByte, true);
        
        var drawProps = MeshDrawProperties.MakeElementalInstance(
            size: DrawElementSize.UnsignedShort,
            primitive:DrawPrimitive.Triangles,
            drawCount: indices.Length,
            instances: instanceCount);

        var meshId = gfx.Meshes.CreateEmptyMesh(in drawProps, 2, attribs);
        gfx.Meshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));
        gfx.Meshes.CreateAttachVertexBuffer(meshId, ReadOnlySpan<FoliageGpuInstance>.Empty,
            CreateVboArgs.MakeInstance(1, 2, instanceCount));
        
        gfx.Meshes.CreateAttachIndexBuffer(meshId, indices, CreateIboArgs.MakeDefault());

    }

}