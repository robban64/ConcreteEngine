using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Engine.Mesh;

[StructLayout(LayoutKind.Sequential)]
public struct FoliageGpuInstance
{
    public Half4 PositionSize;
    public ColorRgba Color;
}

internal sealed class TerrainChunkMesh(int slot, NativeView<Vertex3D> vertices) : IDisposable
{
    private const int ChunkQuads = TerrainChunk.ChunkQuads; // 64
    private const int ChunkSamples = TerrainChunk.ChunkSamples; // 65

    public readonly int Slot = slot;
    
    public MeshId TerrainMeshId;
    public VertexBufferId TerrainVboId;

    public MeshId FoliageMeshId;
    public VertexBufferId FoliageInstanceVboId;

    public BoundingBox Bounds;

    private NativeView<Vertex3D> _vertices = vertices;
    private NativeView<FoliageGpuInstance> _foliageView = NativeView<FoliageGpuInstance>.MakeNull();

    public bool HasNullVertices => _vertices.IsNull;
    public int VertexCount => _vertices.Length;
    public int FoliageCount => _foliageView.Length;
    
    public NativeView<Vertex3D> GetVertices() => _vertices;
    public NativeView<FoliageGpuInstance> GetFoliageInstances() => _foliageView;

    public void Dispose()
    {
        _vertices = NativeView<Vertex3D>.MakeNull();
        _foliageView = NativeView<FoliageGpuInstance>.MakeNull();
    }
    
    internal void CreateChunkMesh(GfxMeshes gfxMeshes, IndexBufferId terrainIboId, int drawCount)
    {
        if (HasNullVertices) throw new InvalidOperationException("Mesh buffer not allocated");

        var args = CreateVboArgs.MakeDynamic(0);
        var props = MeshDrawProperties.MakeElemental(size: DrawElementSize.UnsignedShort, drawCount: drawCount);

        var vertices = _vertices.AsReadOnlySpan();

        var meshId = gfxMeshes.CreateEmptyMesh(in props, 1, VertexAttributes.GetVertex3DAttributes());
        var vboId = gfxMeshes.CreateAttachVertexBuffer(meshId, vertices, args);
        gfxMeshes.AttachIndexBuffer(meshId, terrainIboId);

        TerrainMeshId = meshId;
        TerrainVboId = vboId;
    }
    
     internal unsafe void GenerateFoliageMesh(GfxMeshes gfxMeshes, int instanceCount)
    {
        if(FoliageMeshId > 0) throw new InvalidOperationException("Foliage mesh already generated");
        
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

        var meshId = FoliageMeshId = gfxMeshes.CreateEmptyMesh(in drawProps, 2, attribs);
        
        gfxMeshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));

        var args = CreateVboArgs.MakeInstance(1, 2, instanceCount);
        FoliageInstanceVboId = gfxMeshes.CreateAttachVertexBuffer(meshId, _foliageView.AsReadOnlySpan(), args);
        
        gfxMeshes.CreateAttachIndexBuffer(meshId, indices, CreateIboArgs.MakeDefault());

    }
     
     
    internal int GenerateFoliageBuffer(NativeView<FoliageGpuInstance> instanceData, ReadOnlySpan<byte> data,TerrainChunk chunk, float density)
    {
        if (instanceData.IsNull) throw new ArgumentNullException(nameof(instanceData));
        if(!_foliageView.IsNull) throw new InvalidOperationException("Foliage buffer already allocated");
        
        _foliageView = instanceData;
        var step = 1.0f / density;
        
        var rowStrideBytes = (int)(data.Length / (ChunkQuads * density));

        var random = new FastRandom((uint)chunk.WorldStart.GetHashCode());
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
                instance.PositionSize = new Half4(worldX, y, worldZ, random.RandomFloat(0.8f, 1.2f));
                instance.Color = ColorRgba.White;
                
                instanceCount++;

            }
        }

        return instanceCount;
    }
    
    //FillVertices, GenerateNormals, CalculateBounds
    internal void GenerateHeightBuffer(ReadOnlySpan<byte> heightData, TerrainChunk chunk, int dimension, float maxHeight)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        if (HasNullVertices) throw new InvalidOperationException("Mesh buffer not allocated");

        var vertices = _vertices;

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

                vx.Normal = TerrainUtils.GetNormal(heightData, (int)worldX, (int)worldZ, 1, dimension, maxHeight);
                vx.Tangent =
                    TerrainUtils.GetTangent(heightData, (int)worldX, (int)worldZ, 1, dimension, maxHeight, vx.Normal);
            }
        }

        InvalidOpThrower.ThrowIf(minY > maxY);
        Bounds = new BoundingBox(new Vector3(start.X, minY, start.Y), new Vector3(end.X, maxY, end.Y));
    }
}
