namespace ConcreteEngine.Graphics.Primitives;

public sealed record MeshData
{
    public required Vertex3D[] Vertices { get; init; }
    public required uint[] Indices { get; init; }
}