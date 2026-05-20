using System.Numerics;
using System.Runtime.CompilerServices;
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


internal sealed class TerrainMesh(GfxContext gfx) : IDisposable
{
    private const int Step = 1;
    private const int IndicesPerQuad = 6;
    private const int ChunkQuads = TerrainChunk.ChunkQuads; // 64
    private const int ChunkSamples = TerrainChunk.ChunkSamples; // 65
    private const int IndexCount = ChunkQuads * ChunkQuads * IndicesPerQuad;

    public IndexBufferId TerrainIboId { get; private set; }

    private NativeArray<ushort> _terrainIndexBuffer = NativeArray<ushort>.MakeNull();
    private NativeArray<FoliageGpuInstance> _foliageBuffer = NativeArray<FoliageGpuInstance>.MakeNull();
    
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

        _meshChunks = new TerrainChunkMesh[4 * 4];
        for (var i = 0; i < chunks.Length; i++)
        {
            var it = chunks[i];
            var meshChunk = _meshChunks[i] = new TerrainChunkMesh(i);
            GenerateCompleteVerticesAndBounds(it, meshChunk, data, dimension, maxHeight);
            CreateChunkMesh(meshChunk);
        }
        /*
        for (var i = 0; i < chunks.Length; i++)
        {
            var it = chunks[i];
            var meshChunk = _meshChunks[i];
            var instanceCount = GenerateFoliageBuffer(it, meshChunk);
            GenerateFoliageMesh(meshChunk, instanceCount);
        }
        */

        //FillVertices(it, meshChunk, dimension);
        //GenerateNormals(it, meshChunk, data, dimension, maxHeight);
        //CalculateBounds(it, meshChunk);
    }

    public void AllocateFoliage(ReadOnlySpan<TerrainChunk> chunks, ReadOnlySpan<byte> data)
    {
        const float density = 2.0f;
        const int maxInstanceCount = (int)(ChunkQuads * density * ChunkQuads * density);

        _foliageBuffer = NativeArray.Allocate<FoliageGpuInstance>(chunks.Length * maxInstanceCount);
        for (var i = 0; i < chunks.Length; i++)
        {
            var it = chunks[i];
            var meshChunk = _meshChunks[i];
            meshChunk.SetFoliagePtr(_foliageBuffer.Slice(maxInstanceCount * i, maxInstanceCount * i + maxInstanceCount));
            var instanceCount = GenerateFoliageBuffer(it, meshChunk, data, 2);
            GenerateFoliageMesh(meshChunk, instanceCount);
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
    
    private int GenerateFoliageBuffer(TerrainChunk chunk, TerrainChunkMesh mesh, ReadOnlySpan<byte> data, float density)
    {
        var step = 1.0f / density;
        
        var rowStrideBytes = (int)(data.Length / (ChunkQuads * density));

        var random = new FastRandom((uint)chunk.WorldStart.GetHashCode());
        var instanceData = mesh.GetFoliageInstances();
        var instanceCount = 0;
        for (float z = 0; z < ChunkQuads; z += step)
        {
            for (float x = 0; x < ChunkQuads; x += step)
            {
                float offsetX = random.NextFloat() * step;
                float offsetZ = random.NextFloat() * step;
            
                float worldX = chunk.WorldStart.X + x + offsetX;
                float worldZ = chunk.WorldStart.Y + z + offsetZ;

                var idx = (int)z * rowStrideBytes + (int)x * 4;
                if ((uint)(idx + 4 - 1) >= (uint)data.Length) continue;

                byte r = data[idx];
                if (r / 255f < 0.01f) continue;

                float y = chunk.GetHeight((int)x, (int)z);
                
                int vi = (int)(z * ChunkSamples + x);

                ref var instance = ref instanceData[vi];
                float size = random.RandomFloat(0.8f, 1.2f);
                instance.PositionSize = new Half4(worldX, y, worldZ, size);
                instance.Color = ColorRgba.White;
                
                instanceCount++;

            }
        }

        return instanceCount;
    }
    
    private unsafe void GenerateFoliageMesh(TerrainChunkMesh chunkMesh, int instanceCount)
    {
        var normal = Vector3.UnitY;
        var tangent = Vector3.UnitX; 

        ReadOnlySpan<Vertex3D> vertices = stackalloc Vertex3D[]
        {
            new Vertex3D(new Vector3(-0.5f, 0.0f, -0.5f), new Vector2(0f, 1f), normal, tangent), 
            new Vertex3D(new Vector3( 0.5f, 0.0f,  0.5f), new Vector2(1f, 1f), normal, tangent), 
            new Vertex3D(new Vector3(-0.5f, 1.0f, -0.5f), new Vector2(0f, 0f), normal, tangent), 
            new Vertex3D(new Vector3( 0.5f, 1.0f,  0.5f), new Vector2(1f, 0f), normal, tangent), 

            new Vertex3D(new Vector3(-0.5f, 0.0f,  0.5f), new Vector2(0f, 1f), normal, tangent), 
            new Vertex3D(new Vector3( 0.5f, 0.0f, -0.5f), new Vector2(1f, 1f), normal, tangent), 
            new Vertex3D(new Vector3(-0.5f, 1.0f,  0.5f), new Vector2(0f, 0f), normal, tangent),
            new Vertex3D(new Vector3( 0.5f, 1.0f, -0.5f), new Vector2(1f, 0f), normal, tangent)  
    };
        
        ReadOnlySpan<ushort> indices = stackalloc ushort[]
        {
            // Quad 1
            0, 1, 2,
            2, 1, 3,
            // Quad 2
            4, 5, 6,
            6, 5, 7
        };
        
        Span<VertexAttributeDef> attribs = stackalloc VertexAttributeDef[6];
        VertexAttributes.GetVertex3DAttributes().CopyTo(attribs);
        
        var instanceAttributeMaker = new VertexAttributeMaker();
        attribs[4] = instanceAttributeMaker.Make<Half4>(4, 1, VertexFormat.Half);
        attribs[5] = instanceAttributeMaker.Make<ColorRgba>(5, 1, VertexFormat.UByte, true);
        
        var drawProps = MeshDrawProperties.MakeElementalInstance(
            size: DrawElementSize.UnsignedShort,
            primitive:DrawPrimitive.Triangles,
            drawCount: indices.Length,
            instances: instanceCount);

        var meshId = chunkMesh.FoliageMeshId = gfx.Meshes.CreateEmptyMesh(in drawProps, 2, attribs);
        
        gfx.Meshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));
        
        chunkMesh.FoliageInstanceVboId = gfx.Meshes.CreateAttachVertexBuffer(meshId, 
            chunkMesh.GetFoliageInstances().AsReadOnlySpan(),
            CreateVboArgs.MakeInstance(1, 2, instanceCount));
        
        gfx.Meshes.CreateAttachIndexBuffer(meshId, indices, CreateIboArgs.MakeDefault());

    }

}