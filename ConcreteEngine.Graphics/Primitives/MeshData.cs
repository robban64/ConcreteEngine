namespace ConcreteEngine.Graphics.Primitives;

public sealed record MeshData
{
    public required List<Vertex3D> Vertices { get; init; }
    public required List<uint> Indices { get; init; }
}