using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Models;

public sealed class ModelMesh(
    AssetRef<Model> assetRef,
    string meshName,
    MeshId resourceId,
    int materialSlot,
    int drawCount,
    in Matrix4x4 transform,
    in BoundingBox bounds)
{
    private readonly BoundingBox _bounds = bounds;
    private readonly Matrix4x4 _transform = transform;

    public ref readonly Matrix4x4 Transform => ref _transform;
    public ref readonly BoundingBox Bounds => ref _bounds;

    public AssetRef<Model> AssetRef { get; init; } = assetRef;
    public string MeshName { get; init; } = meshName;
    public MeshId ResourceId { get; init; } = resourceId;
    public int MaterialSlot { get; init; } = materialSlot;
    public int DrawCount { get; init; } = drawCount;
}