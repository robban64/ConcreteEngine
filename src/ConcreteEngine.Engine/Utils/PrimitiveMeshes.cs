using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Utils;

[SuppressMessage("ReSharper", "UseCollectionExpression")]
public static class PrimitiveMeshes
{
    public static MeshId FsqQuad { get; private set; }
    public static MeshId SkyboxCube { get; private set; }

    public static MeshId Cube { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void CreatePrimitives(GfxMeshes meshes)
    {
        InvalidOpThrower.ThrowIf(FsqQuad > 0 || SkyboxCube > 0);
        CreateFsqQuad(meshes);
        CreateSkyboxCube(meshes);
    }

    private static void CreateFsqQuad(GfxMeshes meshes)
    {
        ReadOnlySpan<Vertex2D> vertices = stackalloc[]
        {
            new Vertex2D(-1f, -1f, 0f, 0f), new Vertex2D(1f, -1f, 1f, 0f), new Vertex2D(-1f, 1f, 0f, 1f),
            new Vertex2D(1f, 1f, 1f, 1f)
        };

        var attribBuilder = new VertexAttributeMaker();

        var props = new MeshDrawProperties(DrawPrimitive.TriangleStrip, DrawMeshKind.Arrays, DrawElementSize.Invalid,
            4);

        var meshId = meshes.CreateEmptyMesh(in props, 1, [
            attribBuilder.Make<Vector2>(0), attribBuilder.Make<Vector2>(1)
        ]);
        meshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));
        FsqQuad = meshId;
    }


    private static void CreateSkyboxCube(GfxMeshes meshes)
    {
        ReadOnlySpan<Vector3> vertices = stackalloc Vector3[]
        {
            // +X
            new(1f, 1f, -1f), new(1f, -1f, -1f), new(1f, -1f, 1f), new(1f, 1f, -1f), new(1f, -1f, 1f), new(1f, 1f, 1f),
            // -X
            new(-1f, 1f, 1f), new(-1f, -1f, 1f), new(-1f, -1f, -1f), new(-1f, 1f, 1f), new(-1f, -1f, -1f),
            new(-1f, 1f, -1f),
            // +Y
            new(-1f, 1f, -1f), new(1f, 1f, -1f), new(1f, 1f, 1f), new(-1f, 1f, -1f), new(1f, 1f, 1f), new(-1f, 1f, 1f),
            // -Y
            new(-1f, -1f, 1f), new(1f, -1f, 1f), new(1f, -1f, -1f), new(-1f, -1f, 1f), new(1f, -1f, -1f),
            new(-1f, -1f, -1f),
            // +Z
            new(-1f, 1f, 1f), new(1f, 1f, 1f), new(1f, -1f, 1f), new(-1f, 1f, 1f), new(1f, -1f, 1f), new(-1f, -1f, 1f),
            // -Z
            new(1f, 1f, -1f), new(-1f, 1f, -1f), new(-1f, -1f, -1f), new(1f, 1f, -1f), new(-1f, -1f, -1f),
            new(1f, -1f, -1f)
        };
        var props = new MeshDrawProperties(DrawPrimitive.Triangles, DrawMeshKind.Arrays, DrawElementSize.Invalid, 36);

        var meshId = meshes.CreateEmptyMesh(in props, 1, [
            new VertexAttributeMaker().Make<Vector3>(0)
        ]);
        meshes.CreateAttachVertexBuffer(meshId, vertices, CreateVboArgs.MakeDefault(0));
        SkyboxCube = meshId;
    }
}