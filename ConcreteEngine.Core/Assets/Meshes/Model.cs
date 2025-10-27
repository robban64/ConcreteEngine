#region

using System.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Meshes;

public sealed class Model : AssetObject,  IComparable<Model>
{
    public AssetRef<Model> RefId => new(RawId);
    public required ModelMesh[] MeshParts { get; init; }
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;
    public ResourceKind GfxResourceKind => ResourceKind.Mesh;
    
    
    public int CompareTo(Model? other)
    {
        return other is null ? 1 : RawId.Value.CompareTo(other.RawId.Value);
    }
}

public sealed record ModelMesh(
    AssetRef<Model> AssetRef,
    string MeshName,
    MeshId ResourceId,
    int DrawCount,
    Matrix4x4 Transform);