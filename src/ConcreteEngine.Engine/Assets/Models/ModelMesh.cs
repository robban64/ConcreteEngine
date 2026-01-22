using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Models;

public sealed class ModelMesh(
    string name,
    int meshIndex,
    AssetId assetModel,
    MeshId gfxId,
    int materialIndex,
    int drawCount,
    in BoundingBox localBounds,
    in Matrix4x4 localMatrix)
{
    public readonly string Name = name;
    public readonly AssetId AssetModel = assetModel;
    public readonly MeshId GfxId = gfxId;
    public readonly MeshSpec Spec = new(meshIndex, materialIndex, drawCount);

    public readonly BoundingBox LocalBounds = localBounds;
    public readonly Matrix4x4 LocalMatrix = localMatrix;
}