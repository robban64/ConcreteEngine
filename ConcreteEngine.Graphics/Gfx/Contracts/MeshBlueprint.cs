namespace ConcreteEngine.Graphics.Gfx.Contracts;

public interface IMeshBlueprint
{
    public MeshDrawProperties DrawProperties { get; init; }
    public IReadOnlyList<VertexAttribute> Attributes { get; init; }
    public IReadOnlyList<VertexBufferPayload> VertexBuffers { get; init; }
}

public sealed class MeshBlueprintArray : IMeshBlueprint
{
    public required MeshDrawProperties DrawProperties { get; init; }
    public required IReadOnlyList<VertexAttribute> Attributes { get; init; }
    public required IReadOnlyList<VertexBufferPayload> VertexBuffers { get; init; }
}

public sealed class MeshBlueprintIndexed : IMeshBlueprint
{
    public required MeshDrawProperties DrawProperties { get; init; }
    public required IReadOnlyList<VertexAttribute> Attributes { get; init; }
    public required IReadOnlyList<VertexBufferPayload> VertexBuffers { get; init; }
    public required IndexBufferPayload IndexBuffer { get; init; }
}