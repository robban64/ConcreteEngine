#region

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Graphics.Utils;

public interface IPrimitiveMeshes
{
    MeshId FsqQuad { get; }
    MeshId SkyboxCube { get; }
}

//TODO Move to core.
[SuppressMessage("ReSharper", "UseCollectionExpression")]
internal sealed class PrimitiveMeshes : IPrimitiveMeshes
{
    public MeshId FsqQuad { get; private set; }
    public MeshId SkyboxCube { get; private set; }


    public void CreatePrimitives(GfxMeshes meshes)
    {
        CreateFsqQuad(meshes);
        CreateSkyboxCube(meshes);
    }

    private void CreateFsqQuad(GfxMeshes meshes)
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
        var attribBuilder = new VertexAttributeMaker<Vertex2D>();
        builder.AddAttribute(attribBuilder.Make<Vector2>());
        builder.AddAttribute(attribBuilder.Make<Vector2>());
        FsqQuad = builder.Finish();
    }


    private void CreateSkyboxCube(GfxMeshes meshes)
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
        builder.AddAttribute(new VertexAttributeMaker<Vector3>().Make<Vector3>());
        SkyboxCube = builder.Finish();
    }
}