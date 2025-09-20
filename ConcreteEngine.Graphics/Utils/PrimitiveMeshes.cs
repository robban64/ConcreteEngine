using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Utils;

public interface IPrimitiveMeshes
{
    MeshId FsqQuad { get; }
    MeshId SkyboxCube { get; }
}

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
            new Vertex2D(-1f, -1f, 0f, 0f),
            new Vertex2D(1f, -1f, 1f, 0f),
            new Vertex2D(-1f, 1f, 0f, 1f),
            new Vertex2D(1f, 1f, 1f, 1f)
        };  

        var props = new MeshDrawProperties(DrawPrimitive.TriangleStrip, MeshDrawKind.Arrays, DrawElementSize.Invalid, 4);
        
        var builder = meshes.StartUploadBuilder(in props);
        builder.UploadVertices(vertices, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.AddAttribute(VertexAttributeDesc.Make<Vertex2D>(nameof(Vertex2D.Position), VertexElementFormat.Float2));
        builder.AddAttribute(VertexAttributeDesc.Make<Vertex2D>(nameof(Vertex2D.TexCoords), VertexElementFormat.Float2));
        FsqQuad = builder.Finish();

    }


    private void CreateSkyboxCube(GfxMeshes meshes)
    {
        ReadOnlySpan<float> vertices = stackalloc[]
        {
            // +X
            1f, 1f, -1f, 1f, -1f, -1f, 1f, -1f, 1f,
            1f, 1f, -1f, 1f, -1f, 1f, 1f, 1f, 1f,
            // -X
            -1f, 1f, 1f, -1f, -1f, 1f, -1f, -1f, -1f,
            -1f, 1f, 1f, -1f, -1f, -1f, -1f, 1f, -1f,
            // +Y
            -1f, 1f, -1f, 1f, 1f, -1f, 1f, 1f, 1f,
            -1f, 1f, -1f, 1f, 1f, 1f, -1f, 1f, 1f,
            // -Y
            -1f, -1f, 1f, 1f, -1f, 1f, 1f, -1f, -1f,
            -1f, -1f, 1f, 1f, -1f, -1f, -1f, -1f, -1f,
            // +Z
            -1f, 1f, 1f, 1f, 1f, 1f, 1f, -1f, 1f,
            -1f, 1f, 1f, 1f, -1f, 1f, -1f, -1f, 1f,
            // -Z
            1f, 1f, -1f, -1f, 1f, -1f, -1f, -1f, -1f,
            1f, 1f, -1f, -1f, -1f, -1f, 1f, -1f, -1f,
        };
        
        var props = new MeshDrawProperties(DrawPrimitive.Triangles, MeshDrawKind.Arrays, DrawElementSize.Invalid, 36);
        
        var builder = meshes.StartUploadBuilder(in props);
        builder.UploadVertices(vertices, BufferUsage.StaticDraw, BufferStorage.Static, BufferAccess.None);
        builder.AddAttribute(new VertexAttributeDesc(0,sizeof(float) * 3, 0, VertexElementFormat.Float3));
        SkyboxCube = builder.Finish();
    }
}