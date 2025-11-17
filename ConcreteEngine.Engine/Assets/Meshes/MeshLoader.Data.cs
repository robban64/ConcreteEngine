#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Engine.Assets.Meshes;

public readonly ref struct MeshImportData(
    ReadOnlySpan<Vertex3D> vertices,
    ReadOnlySpan<uint> indices)
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

internal struct MeshImportResult(
    int materialSlot,
    MeshCreationInfo creationInfo,
    in BoundingBox bounds)
{
    public int MaterialSlot = materialSlot;
    public MeshCreationInfo CreationInfo = creationInfo;
    public BoundingBox Bounds = bounds;
}

internal sealed class ModelImportResult
{
    public List<string> PartNames { get; } = new(4);
    public int Parts { get; set; }
    public BoundingBox Bounds { get; set; } 
}

internal sealed record MeshPartImportResult(
    string Name,
    int MaterialSlot,
    MeshCreationInfo Info,
    in BoundingBox Box,
    in Matrix4x4 Transform)
{
    private readonly Matrix4x4 _transform = Transform;
    public ref readonly Matrix4x4 Transform => ref _transform;
}