using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class SceneTransform(SceneObject sceneObject)
{
    private Transform _transform;
    private BoundingBox _bounds;

    public ref readonly Transform GetTransform() => ref _transform;
    public ref readonly BoundingBox GetBounds() => ref _bounds;

    public void GetTransformMatrix(out Matrix4x4 matrix) => MatrixMath.CreateModelMatrix(in _transform, out matrix);

    //
    [InputNumber(Format = "%.3f")]
    public Vector3 Translation
    {
        get => _transform.Translation;
        set
        {
            _transform.Translation = value;
            sceneObject.MarkDirty(SceneDirtyFlags.Transform);
        }
    }

    [InputNumber(Format = "%.3f")]
    public Vector3 Scale
    {
        get => _transform.Scale;
        set
        {
            _transform.Scale = value;
            sceneObject.MarkDirty(SceneDirtyFlags.Transform);
        }
    }

    [InputNumber(Format = "%.3f")]
    public Quaternion Rotation
    {
        get => _transform.Rotation;
        set
        {
            _transform.Rotation = value;
            sceneObject.MarkDirty(SceneDirtyFlags.Transform);
        }
    }

    //
    public void SetTransform(in Transform transform)
    {
        _transform = transform;
        sceneObject.MarkDirty(SceneDirtyFlags.Transform);
    }

    public void SetBounds(in BoundingBox bounds)
    {
        _bounds = bounds;
        sceneObject.MarkDirty(SceneDirtyFlags.Transform);
    }
}