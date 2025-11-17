#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Assets.Meshes;

public sealed class Model : AssetObject, IComparable<Model>
{
    public ModelId ModelId { get; private set; } = default;
    public required ModelMesh[] MeshParts { get; init; }
    public required int DrawCount { get; init; }
    public required BoundingBox Bounds { get; init; }

    public AssetRef<Model> RefId => new(RawId);
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;
    public ResourceKind GfxResourceKind => ResourceKind.Mesh;

    public ModelBaseDrawInfo ToBaseDrawInfo() => new(ModelId, MeshParts.Length, DrawCount);

    internal void AttachToRenderer(ModelId modelId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(modelId.Value, 0, nameof(modelId));
        InvalidOpThrower.ThrowIf(ModelId.Value > 0, nameof(ModelId));
        ModelId = modelId;
    }

    public int CompareTo(Model? other)
    {
        return other is null ? 1 : RawId.Value.CompareTo(other.RawId.Value);
    }
}

public readonly record struct ModelBaseDrawInfo(ModelId Model, int PartCount, int DrawCount);

public sealed record ModelMesh(
    AssetRef<Model> AssetRef,
    string MeshName,
    MeshId ResourceId,
    int MaterialSlot,
    int DrawCount,
    in Matrix4x4 Transform,
    in BoundingBox Bounds)
{
    private readonly BoundingBox _bounds = Bounds;
    private readonly Matrix4x4 _transform = Transform;

    public ref readonly Matrix4x4 Transform => ref _transform;
    public ref readonly BoundingBox Bounds => ref _bounds;

}

public sealed record RecordObject(string SomeName, in Matrix4x4 Matrix)
{
    private readonly Matrix4x4 _matrix = Matrix;

    public ref readonly Matrix4x4 Matrix => ref _matrix;
}
/*
    IL_0000: ldarg.1      // SomeName
   IL_0001: ldarg.0      // this
   IL_0002: call         instance string ConcreteEngine.Engine.Assets.Meshes.RecordObject::get_SomeName()
   IL_0007: stind.ref
   IL_0008: ldarg.2      // Matrix
   IL_0009: ldarg.0      // this
   IL_000a: call         instance valuetype [System.Numerics.Vectors]System.Numerics.Matrix4x4& modreq ([System.Runtime]System.Runtime.InteropServices.InAttribute) ConcreteEngine.Engine.Assets.Meshes.RecordObject::get_Matrix()
   IL_000f: ldobj        [System.Numerics.Vectors]System.Numerics.Matrix4x4
   IL_0014: stobj        [System.Numerics.Vectors]System.Numerics.Matrix4x4
   IL_0019: ret

*/