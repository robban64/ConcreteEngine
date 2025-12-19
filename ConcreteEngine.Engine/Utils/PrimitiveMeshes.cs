using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Utils;

[SuppressMessage("ReSharper", "UseCollectionExpression")]
public static class PrimitiveMeshes
{
    public static MeshId FsqQuad { get; private set; }
    public static MeshId SkyboxCube { get; private set; }

    public static MeshId Cube { get; set; }

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

        var props = new MeshDrawProperties(DrawPrimitive.TriangleStrip, DrawMeshKind.Arrays, DrawElementSize.Invalid,
            4);
        var builder = meshes.StartUploadBuilder(in props);
        builder.UploadVertices(vertices, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        var attribBuilder = new VertexAttributeMaker();
        builder.AddAttribute(attribBuilder.Make<Vector2>(0));
        builder.AddAttribute(attribBuilder.Make<Vector2>(1));
        FsqQuad = meshes.FinishUploadBuilder(out _);
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

        var builder = meshes.StartUploadBuilder(in props);
        builder.UploadVertices(vertices, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.AddAttribute(new VertexAttributeMaker().Make<Vector3>(0));
        SkyboxCube = meshes.FinishUploadBuilder(out _);
    }
}