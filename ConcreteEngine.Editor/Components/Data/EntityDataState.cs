#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Editor.Components.Data;

public struct EntityDataState
{
    public int EntityId;
    public int ModelId;
    public int MaterialTagKey;
    internal TransformDataState Transform;
    public BoundingBox Bounds;

    public void SetTransform(in TransformData transform) => Transform.From(in transform);
    public readonly TransformData GetTransform() => Transform.AsTransformData();
}