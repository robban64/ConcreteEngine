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
        Span<Vertex2D> vertices = stackalloc[]
        {
            new Vertex2D(-1f, -1f, 0f, 0f),
            new Vertex2D(1f, -1f, 1f, 0f),
            new Vertex2D(-1f, 1f, 0f, 1f),
            new Vertex2D(1f, 1f, 1f, 1f)
        };

        var desc = new MeshDescriptor<Vertex2D, uint>
        {
            VertexBuffer = new MeshDataBufferDescriptor<Vertex2D>(BufferUsage.StaticDraw, null),
            VertexPointers =
            [
                VertexAttributeDescriptor.Make<Vertex2D>(nameof(Vertex2D.Position), VertexElementFormat.Float2),
                VertexAttributeDescriptor.Make<Vertex2D>(nameof(Vertex2D.TexCoords), VertexElementFormat.Float2)
            ],
            Primitive = DrawPrimitive.TriangleStrip,
            DrawKind = MeshDrawKind.Arrays,
            DrawCount = 4
        };

        FsqQuad = graphics.CreateMesh(desc, out var meta);
        graphics.Gfx.BindVertexBuffer(meta.VertexBufferId);
        graphics.Gfx.SetVertexBuffer<Vertex2D>(vertices);
        graphics.Gfx.BindVertexBuffer(default);
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

        var desc = new MeshDescriptor<Vertex2D, uint>
        {
            VertexBuffer = new MeshDataBufferDescriptor<Vertex2D>(BufferUsage.StaticDraw, null),
            VertexPointers =
            [
                new VertexAttributeDescriptor(sizeof(float) * 3, 0, VertexElementFormat.Float3),
            ],
            DrawKind = MeshDrawKind.Arrays,
            Primitive = DrawPrimitive.Triangles,
            DrawCount = 36
        };

        SkyboxCube = graphics.CreateMesh(desc, out var meta);
        graphics.Gfx.BindVertexBuffer(meta.VertexBufferId);
        graphics.Gfx.SetVertexBuffer<float>(vertices);
        graphics.Gfx.BindVertexBuffer(default);
    }
}