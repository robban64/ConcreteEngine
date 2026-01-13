using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets.Models;

public sealed class ModelMesh(
    string name,
    int meshIndex,
    AssetId assetModel,
    MeshId gfxId,
    int materialSlot,
    int drawCount,
    in BoundingBox localBounds,
    in Matrix4x4 localMatrix)
{
    public string Name { get; init; } = name;
    public int MeshIndex { get; init; } = meshIndex;

    public AssetId AssetModel { get; init; } = assetModel;
    public MeshId GfxId { get; init; } = gfxId;

    public int MaterialSlot { get; init; } = materialSlot;
    public int DrawCount { get; init; } = drawCount;

    public readonly BoundingBox LocalBounds = localBounds;
    public readonly Matrix4x4 LocalMatrix = localMatrix;
}