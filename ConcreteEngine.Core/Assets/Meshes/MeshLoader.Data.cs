#region

using System.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Core.Assets.Meshes;

public readonly ref struct MeshImportData(ReadOnlySpan<Vertex3D> vertices, ReadOnlySpan<uint> indices)
{
    public readonly ReadOnlySpan<Vertex3D> Vertices = vertices;
    public readonly ReadOnlySpan<uint> Indices = indices;
}

internal readonly ref struct MeshUploadPayload(
    ReadOnlySpan<VertexAttribute> attributes,
    ReadOnlySpan<Vertex3D> vertices,
    ReadOnlySpan<uint> indices,
    MeshDrawProperties properties)
{
    public readonly ReadOnlySpan<VertexAttribute> Attributes = attributes;
    public readonly ReadOnlySpan<Vertex3D> Vertices = vertices;
    public readonly ReadOnlySpan<uint> Indices = indices;
    public readonly MeshDrawProperties Properties = properties;
}

internal readonly record struct MeshCreationInfo(MeshId MeshId, int DrawCount);

internal sealed record MeshPartImportResult(
    string Name,
    int MaterialSlot,
    MeshCreationInfo Info,
    in Matrix4x4 Transform);

