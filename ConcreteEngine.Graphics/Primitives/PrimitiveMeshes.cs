using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Primitives;

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
    

    internal void CreatePrimitives(IGraphicsDevice graphics)
    {
        CreateFsqQuad(graphics);
        CreateSkyboxCube(graphics);
    }

    private void CreateFsqQuad(IGraphicsDevice graphics)
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

        var metaDesc = new GpuMeshDescriptor
        {
            VertexPointers = pointers,
            Primitive = DrawPrimitive.TriangleStrip,
            DrawKind = MeshDrawKind.Arrays,
            DrawCount = 4
        };

        var dataDesc = new GpuMeshData<Vertex2D, uint>(vertices);

        FsqQuad = graphics.CreateMesh(in dataDesc, in metaDesc, out _);
    }


    private void CreateSkyboxCube(IGraphicsDevice graphics)
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
            new VertexAttributeDescriptor(sizeof(float) * 3, 0, VertexElementFormat.Float3),
        };
        
        var dataDesc = new GpuMeshData<float, uint>(vertices);

        var metaDesc = new GpuMeshDescriptor
        {
            VertexPointers = pointers,
            DrawKind = MeshDrawKind.Arrays,
            Primitive = DrawPrimitive.Triangles,
            DrawCount = 36
        };

        SkyboxCube = graphics.CreateMesh(in dataDesc, in metaDesc, out _);
    }
}