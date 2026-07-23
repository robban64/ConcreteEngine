using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Enviroment;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Engine.Mesh;

[StructLayout(LayoutKind.Sequential)]
public struct FoliageGpuInstance
{
    public Half4 PositionSize;
    public ColorRgba Color;
}

internal sealed class TerrainChunkMesh(int slot)
{
    private const int ChunkQuads = TerrainChunk.ChunkQuads; // 64
    private const int ChunkSamples = TerrainChunk.ChunkSamples; // 65

    public readonly int Slot = slot;

    public MeshId TerrainMeshId;

    public MeshId FoliageMeshId;
    public VertexBufferId FoliageInstanceVboId;
    public float FoliageDensity;

    public int FoliageCount;

    internal void CreateChunkMesh(
        GfxMeshes gfx,
        IndexBufferId terrainIboId,
        NativeView<Vector3> vPos,
        NativeView<VertexShading> vShading,
        int drawCount)
    {
        var props = MeshDrawProperties.MakeElemental(size: DrawElementSize.UnsignedShort, drawCount: drawCount);

        var meshId = gfx.CreateEmptyMesh(in props, 2, VertexAttributes.GetVertex3DAttributesV2());
        gfx.CreateAttachVertexBuffer(meshId, vPos.AsReadOnlySpan(), CreateVboArgs.MakeDynamic(0));
        gfx.CreateAttachVertexBuffer(meshId, vShading.AsReadOnlySpan(), CreateVboArgs.MakeDynamic(1));

        gfx.AttachIndexBuffer(meshId, terrainIboId);

        TerrainMeshId = meshId;
    }

    internal unsafe void GenerateFoliageMesh(GfxMeshes gfxMeshes, NativeView<FoliageGpuInstance> foliage, int instanceCount)
    {
        if (foliage.IsNull) Throwers.NullPointer(nameof(foliage));
        if (FoliageMeshId > 0) Throwers.InvalidOperation("Foliage mesh already generated");

        var normal = Vector3.UnitY;
        var tangent = Vector3.UnitX;

        ReadOnlySpan<Vertex3D> vertices = stackalloc Vertex3D[]
        {
            new Vertex3D(new Vector3(-0.5f, 0.0f, -0.5f), new Vector2(0f, 1f), normal, tangent),
            new Vertex3D(new Vector3(0.5f, 0.0f, 0.5f), new Vector2(1f, 1f), normal, tangent),
            new Vertex3D(new Vector3(-0.5f, 1.0f, -0.5f), new Vector2(0f, 0f), normal, tangent),
            new Vertex3D(new Vector3(0.5f, 1.0f, 0.5f), new Vector2(1f, 0f), normal, tangent),
            new Vertex3D(new Vector3(-0.5f, 0.0f, 0.5f), new Vector2(0f, 1f), normal, tangent),
            new Vertex3D(new Vector3(0.5f, 0.0f, -0.5f), new Vector2(1f, 1f), normal, tangent),
            new Vertex3D(new Vector3(-0.5f, 1.0f, 0.5f), new Vector2(0f, 0f), normal, tangent),
            new Vertex3D(new Vector3(0.5f, 1.0f, -0.5f), new Vector2(1f, 0f), normal, tangent)
        };

        ReadOnlySpan<ushort> indices = stackalloc ushort[]
        {
            // Quad 1
            0, 1, 2, 2, 1, 3,
            // Quad 2
            4, 5, 6, 6, 5, 7
        };

        Span<VertexAttributeDef> attribs = stackalloc VertexAttributeDef[7];
        VertexAttributes.GetVertex3DAttributes().CopyTo(attribs);

        var instanceAttributeMaker = new VertexAttributeMaker();
        attribs[4] = instanceAttributeMaker.Make<Half4>(location: 4, binding: 1, VertexFormat.Half);
        attribs[5] = instanceAttributeMaker.Make(stride: 3, location: 5, binding: 1, VertexFormat.UByte, true);
        attribs[6] = instanceAttributeMaker.Make<byte>(location: 6, binding: 1, VertexFormat.UByte);

        var drawProps = MeshDrawProperties.MakeElementalInstance(
            size: DrawElementSize.UnsignedShort,
            primitive: DrawPrimitive.Triangles,
            drawCount: indices.Length,
            instances: instanceCount);

        var meshId = FoliageMeshId = gfxMeshes.CreateEmptyMesh(in drawProps, 2, attribs);

        gfxMeshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));

        var args = CreateVboArgs.MakeInstance(1, 2, instanceCount);
        FoliageInstanceVboId = gfxMeshes.CreateAttachVertexBuffer(meshId, foliage.AsReadOnlySpan(), args);

        gfxMeshes.CreateAttachIndexBuffer(meshId, indices, CreateIboArgs.MakeDefault());
    }


    internal int GenerateFoliageBuffer(
        Terrain terrain,
        TerrainChunk chunk,
        NativeView<FoliageGpuInstance> foliage,
        ReadOnlySpan<byte> heights,
        float density)
    {
        if (foliage.IsNull) Throwers.NullPointer(nameof(foliage));

        FoliageCount = foliage.Length;
        FoliageDensity = density;

        var step = 1.0f / density;

        var start = chunk.WorldStart;
        var dimensions = terrain.Dimension - 1;

        var random = new FastRandom((uint)chunk.WorldStart.GetHashCode());
        var instanceCount = 0;
        for (float z = 0; z < ChunkQuads; z += step)
        for (float x = 0; x < ChunkQuads; x += step)
        {
            float offsetX = random.NextFloat() * step, offsetZ = random.NextFloat() * step;
            float worldX = start.X + x + offsetX, worldZ = start.Y + z + offsetZ;

            var layer = TerrainUtils.SampleLayer(heights, (int)(start.X + x), (int)(start.Y + z), dimensions);
            if (layer < 0) continue;

            float y = terrain.GetHeight(worldX, worldZ);

            int vi = (int)(z * ChunkSamples + x);
            ref var instance = ref foliage[vi];
            
            instance.PositionSize = new Half4(worldX, y, worldZ, random.RandomFloat(0.8f, 1.2f));
            instance.Color.R = (byte)(255f * random.RandomFloat(0.8f, 1f));
            instance.Color.G = (byte)(255f * random.RandomFloat(0.8f, 1f));
            instance.Color.B = (byte)(255f * random.RandomFloat(0.8f, 1f));
            instance.Color.A = (byte)layer;

            ++instanceCount;
        }
        

        return instanceCount;
    }

    //FillVertices, GenerateNormals, CalculateBounds
    internal void GenerateHeightBuffer(
        TerrainChunk chunk,
        NativeView<Vector3> vPos,
        NativeView<VertexShading> vShading,
        ReadOnlySpan<byte> heights,
        int dimension,
        float maxHeight)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        if (vPos.IsNull || vShading.IsNull)
            Throwers.NullPointer("Null vPos or vShading");
        if (vPos.Length != vShading.Length || vPos.Length == 0)
            Throwers.NullPointer("vPos and vShading length mismatch");

        var start = chunk.WorldStart;
        for (int z = 0; z < ChunkSamples; z++)
        for (int x = 0; x < ChunkSamples; x++)
        {
            float worldX = start.X + x, worldZ = start.Y + z;
            float y = chunk.GetHeight(x, z);

            int vi = z * ChunkSamples + x;
            vPos[vi] = new Vector3(worldX, y, worldZ);
        }

        for (int z = 0; z < ChunkSamples; z++)
        for (int x = 0; x < ChunkSamples; x++)
        {
            float worldX = start.X + x, worldZ = start.Y + z;
            float u = worldX / (dimension - 1), v = worldZ / (dimension - 1);

            int vi = z * ChunkSamples + x;
            ref var vx = ref vShading[vi];

            vx.TexCoords = new Vector2(u, v);
            vx.Normal = TerrainUtils
                .GetNormal(heights, (int)worldX, (int)worldZ, 1, dimension, maxHeight);
            vx.Tangent = TerrainUtils
                .GetTangent(heights, (int)worldX, (int)worldZ, 1, dimension, maxHeight, vx.Normal);
        }
    }

/*
    internal void FillVertices(TerrainChunk chunk, NativeView<Vector3> vPos, NativeView<VertexShading> vShading, int dimension)
    {
        if (HasNullVertices) throw new InvalidOperationException("Mesh buffer not allocated");

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

    internal void GenerateNormals(TerrainChunk chunk, ReadOnlySpan<byte> data, int dimension, float maxHeight)
    {
        if (HasNullVertices) throw new InvalidOperationException("Mesh buffer not allocated");

        var vertices = _vertices;

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
    */
}