using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics;

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
    

    internal void CreatePrimitives(IMeshFactory factory)
    {
        CreateFsqQuad(factory);
        CreateSkyboxCube(factory);
    }

    private void CreateFsqQuad(IMeshFactory factory)
    {
        ReadOnlySpan<Vertex2D> vertices = stackalloc[]
        {
            new Vertex2D(-1f, -1f, 0f, 0f),
            new Vertex2D(1f, -1f, 1f, 0f),
            new Vertex2D(-1f, 1f, 0f, 1f),
            new Vertex2D(1f, 1f, 1f, 1f)
        };

        ReadOnlySpan<VertexAttributeDescriptor> pointers = stackalloc[]
        {
            VertexAttributeDescriptor.Make<Vertex2D>(nameof(Vertex2D.Position), VertexElementFormat.Float2),
            VertexAttributeDescriptor.Make<Vertex2D>(nameof(Vertex2D.TexCoords), VertexElementFormat.Float2)
        };
        var metaDesc = GpuMeshDescriptor.MakeArray(pointers, DrawPrimitive.TriangleStrip, 4);
        var vbo = new GpuVboDescriptor<Vertex2D>(vertices, BufferUsage.StaticDraw);

        var result = factory.CreateArrayMesh(vbo, metaDesc);

        FsqQuad = result.MeshId;
    }


    private void CreateSkyboxCube(IMeshFactory factory)
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

        
        ReadOnlySpan<VertexAttributeDescriptor> pointers = stackalloc[]
        {
            new VertexAttributeDescriptor(0,sizeof(float) * 3, 0, VertexElementFormat.Float3),
        };
        
        var dataDesc = new GpuVboDescriptor<float>(vertices, BufferUsage.StaticDraw);
        var metaDesc = GpuMeshDescriptor.MakeArray(pointers, DrawPrimitive.Triangles, 36);

        var result = factory.CreateArrayMesh(dataDesc, metaDesc);
        SkyboxCube = result.MeshId;
    }
}