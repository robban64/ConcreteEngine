using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Models;

public sealed class ModelMesh(
    AssetId assetId,
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

    public AssetId AssetId { get; init; } = assetId;
    public string MeshName { get; init; } = meshName;
    public MeshId ResourceId { get; init; } = resourceId;
    public int MaterialSlot { get; init; } = materialSlot;
    public int DrawCount { get; init; } = drawCount;
}