using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Graphics.Utility;

[SuppressMessage("ReSharper", "UseCollectionExpression")]
internal static class PrimitiveMeshBuilder
{
    public static unsafe MeshId GenerateFsqQuad(GfxMeshes meshes)
    {
        Vertex2D* vertexPtr = stackalloc[]
        {
            new Vertex2D(-1f, -1f, 0f, 0f),
            //
            new Vertex2D(1f, -1f, 1f, 0f),
            //
            new Vertex2D(-1f, 1f, 0f, 1f),
            //
            new Vertex2D(1f, 1f, 1f, 1f)
        };
        var vertices = new NativeView<Vertex2D>(vertexPtr, 4);

        var props = new MeshDrawProperties(
            DrawPrimitive.TriangleStrip,
            DrawMeshKind.Arrays,
            DrawElementSize.None,
            4);

        var attribBuilder = new VertexAttributeMaker();
        var meshId = meshes.CreateEmptyMesh(in props, 1, [
            attribBuilder.Make<Vector2>(0), attribBuilder.Make<Vector2>(1)
        ]);
        meshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));
        return meshId;
    }


    public static unsafe MeshId GenerateSkyboxCube(GfxMeshes meshes)
    {
        var vertexPtr = stackalloc Vector3[36]
        {
            // +X
            new Vector3(1f, 1f, -1f), new Vector3(1f, -1f, -1f), new Vector3(1f, -1f, 1f),
            //
            new Vector3(1f, 1f, -1f), new Vector3(1f, -1f, 1f), new Vector3(1f, 1f, 1f),
            // -X
            new Vector3(-1f, 1f, 1f), new Vector3(-1f, -1f, 1f), new Vector3(-1f, -1f, -1f),
            //
            new Vector3(-1f, 1f, 1f), new Vector3(-1f, -1f, -1f), new Vector3(-1f, 1f, -1f),
            // +Y
            new Vector3(-1f, 1f, -1f), new Vector3(1f, 1f, -1f), new Vector3(1f, 1f, 1f),
            //
            new Vector3(-1f, 1f, -1f), new Vector3(1f, 1f, 1f), new Vector3(-1f, 1f, 1f),
            // -Y
            new Vector3(-1f, -1f, 1f), new Vector3(1f, -1f, 1f), new Vector3(1f, -1f, -1f),
            //
            new Vector3(-1f, -1f, 1f), new Vector3(1f, -1f, -1f), new Vector3(-1f, -1f, -1f),
            // +Z
            new Vector3(-1f, 1f, 1f), new Vector3(1f, 1f, 1f), new Vector3(1f, -1f, 1f),
            //
            new Vector3(-1f, 1f, 1f), new Vector3(1f, -1f, 1f), new Vector3(-1f, -1f, 1f),
            // -Z
            new Vector3(1f, 1f, -1f), new Vector3(-1f, 1f, -1f), new Vector3(-1f, -1f, -1f),
            //
            new Vector3(1f, 1f, -1f), new Vector3(-1f, -1f, -1f), new Vector3(1f, -1f, -1f)
        };
        var vertices = new NativeView<Vector3>(vertexPtr, 36);
        var props = new MeshDrawProperties(DrawPrimitive.Triangles, DrawMeshKind.Arrays, DrawElementSize.None, 36);

        var meshId = meshes.CreateEmptyMesh(in props, 1, [
            new VertexAttributeMaker().Make<Vector3>(0)
        ]);
        meshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));
        return meshId;
    }

    public static unsafe MeshId GenerateCube(GfxMeshes meshes)
    {
        Vertex3D* verticesPtr = stackalloc Vertex3D[24]
        {
            // Front (normal: 0, 0, 1)
            new Vertex3D(new Vector3(-1f, -1f, 1f), new Vector2(0f, 0f), new Vector3(0f, 0f, 1f)),
            new Vertex3D(new Vector3(1f, -1f, 1f), new Vector2(1f, 0f), new Vector3(0f, 0f, 1f)),
            new Vertex3D(new Vector3(1f, 1f, 1f), new Vector2(1f, 1f), new Vector3(0f, 0f, 1f)),
            new Vertex3D(new Vector3(-1f, 1f, 1f), new Vector2(0f, 1f), new Vector3(0f, 0f, 1f)),

            // Back (normal: 0, 0, -1)
            new Vertex3D(new Vector3(1f, -1f, -1f), new Vector2(0f, 0f), new Vector3(0f, 0f, -1f)),
            new Vertex3D(new Vector3(-1f, -1f, -1f), new Vector2(1f, 0f), new Vector3(0f, 0f, -1f)),
            new Vertex3D(new Vector3(-1f, 1f, -1f), new Vector2(1f, 1f), new Vector3(0f, 0f, -1f)),
            new Vertex3D(new Vector3(1f, 1f, -1f), new Vector2(0f, 1f), new Vector3(0f, 0f, -1f)),

            // Top (normal: 0, 1, 0)
            new Vertex3D(new Vector3(-1f, 1f, 1f), new Vector2(0f, 0f), new Vector3(0f, 1f, 0f)),
            new Vertex3D(new Vector3(1f, 1f, 1f), new Vector2(1f, 0f), new Vector3(0f, 1f, 0f)),
            new Vertex3D(new Vector3(1f, 1f, -1f), new Vector2(1f, 1f), new Vector3(0f, 1f, 0f)),
            new Vertex3D(new Vector3(-1f, 1f, -1f), new Vector2(0f, 1f), new Vector3(0f, 1f, 0f)),

            // Bottom (normal: 0, -1, 0)
            new Vertex3D(new Vector3(-1f, -1f, -1f), new Vector2(0f, 0f), new Vector3(0f, -1f, 0f)),
            new Vertex3D(new Vector3(1f, -1f, -1f), new Vector2(1f, 0f), new Vector3(0f, -1f, 0f)),
            new Vertex3D(new Vector3(1f, -1f, 1f), new Vector2(1f, 1f), new Vector3(0f, -1f, 0f)),
            new Vertex3D(new Vector3(-1f, -1f, 1f), new Vector2(0f, 1f), new Vector3(0f, -1f, 0f)),

            // Right (normal: 1, 0, 0)
            new Vertex3D(new Vector3(1f, -1f, 1f), new Vector2(0f, 0f), new Vector3(1f, 0f, 0f)),
            new Vertex3D(new Vector3(1f, -1f, -1f), new Vector2(1f, 0f), new Vector3(1f, 0f, 0f)),
            new Vertex3D(new Vector3(1f, 1f, -1f), new Vector2(1f, 1f), new Vector3(1f, 0f, 0f)),
            new Vertex3D(new Vector3(1f, 1f, 1f), new Vector2(0f, 1f), new Vector3(1f, 0f, 0f)),

            // Left (normal: -1, 0, 0)
            new Vertex3D(new Vector3(-1f, -1f, -1f), new Vector2(0f, 0f), new Vector3(-1f, 0f, 0f)),
            new Vertex3D(new Vector3(-1f, -1f, 1f), new Vector2(1f, 0f), new Vector3(-1f, 0f, 0f)),
            new Vertex3D(new Vector3(-1f, 1f, 1f), new Vector2(1f, 1f), new Vector3(-1f, 0f, 0f)),
            new Vertex3D(new Vector3(-1f, 1f, -1f), new Vector2(0f, 1f), new Vector3(-1f, 0f, 0f))
        };

        uint* indicesPtr = stackalloc uint[36]
        {
            //  Front
            0, 1, 2, 0, 2, 3,
            //  Back
            4, 5, 6, 4, 6, 7,
            //  Top
            8, 9, 10, 8, 10, 11,
            //  Bottom
            12, 13, 14, 12, 14, 15,
            //  Right
            16, 17, 18, 16, 18, 19,
            //  Left
            20, 21, 22, 20, 22, 23
        };
        var vertices = new NativeView<Vertex3D>(verticesPtr, 24);
        var indices = new NativeView<uint>(indicesPtr, 24);

        var props = MeshDrawProperties.MakeElemental(drawCount: indices.Length);
        var attribBuilder = new VertexAttributeMaker();
        var meshId = meshes.CreateEmptyMesh(in props, 1,
        [
            attribBuilder.Make<Vector3>(0), attribBuilder.Make<Vector2>(1), attribBuilder.Make<Vector3>(2)
        ]);
        meshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));
        meshes.CreateAttachIndexBuffer(meshId, indices, CreateIboArgs.MakeDefault());
        return meshId;
    }

    public static unsafe MeshId GenerateSphere(GfxMeshes meshes, float radius, int rings, int sectors)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rings, 2);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sectors, 2);
        ArgumentOutOfRangeException.ThrowIfNotEqual(rings % 2, 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(sectors % 2, 0);

        var ringStep = 1.0f / (rings - 1);
        var sectorStep = 1.0f / (sectors - 1);

        var vertexCount = rings * sectors;
        var indexCount = (rings - 1) * (sectors - 1) * 4;

        Vertex3D* vertexPtr = stackalloc Vertex3D[vertexCount];
        uint* indexPtr = stackalloc uint[indexCount];
        var vertices = new NativeView<Vertex3D>(vertexPtr, vertexCount);
        var indices = new NativeView<uint>(indexPtr, indexCount);
        for (var r = 0; r < rings; r++)
        {
            for (var s = 0; s < sectors; s++)
            {
                var y = MathF.Sin(-MathF.PI / 2f + MathF.PI * r * ringStep);
                var x = MathF.Cos(2f * MathF.PI * s * sectorStep) * MathF.Sin(MathF.PI * r * ringStep);
                var z = MathF.Sin(2f * MathF.PI * s * sectorStep) * MathF.Sin(MathF.PI * r * ringStep);

                vertices[r * sectors + s] = new Vertex3D
                {
                    Position = new Vector3(x, y, z) * radius,
                    TexCoords = new Vector2(s * sectorStep, r * ringStep),
                    Normal = new Vector3(x, y, z)
                };
            }
        }

        var index = 0;
        for (var r = 0; r < rings - 1; r++)
        {
            for (var s = 0; s < sectors - 1; s++)
            {
                indices[index++] = (uint)(r * sectors + s);
                indices[index++] = (uint)(r * sectors + s + 1);
                indices[index++] = (uint)((r + 1) * sectors + s + 1);
                indices[index++] = (uint)((r + 1) * sectors + s);
            }
        }

        var props = MeshDrawProperties.MakeElemental(drawCount: indices.Length);
        var attribBuilder = new VertexAttributeMaker();
        var meshId = meshes.CreateEmptyMesh(in props, 1,
        [
            attribBuilder.Make<Vector3>(0), attribBuilder.Make<Vector2>(1), attribBuilder.Make<Vector3>(2)
        ]);
        meshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));
        meshes.CreateAttachIndexBuffer(meshId, indices, CreateIboArgs.MakeDefault());
        return meshId;
    }
}