using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Core.Engine.RenderEntity;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class SceneTransform(SceneObject sceneObject)
{
    private Transform _transform;
    private BoundingAxisBox _bounds;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly Transform GetTransform() => ref _transform;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BoundingAxisBox GetBounds() => ref _bounds;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetTransformMatrix(out Matrix4x4 matrix) => MatrixMath.CreateModelMatrix(in _transform, out matrix);

    //
    [InputNumber]
    public Vector3 Translation
    {
        get => _transform.Translation;
        set
        {
            _transform.Translation = value;
            sceneObject.MarkDirty(SceneDirtyFlags.Transform);
        }
    }

    [InputNumber]
    public Vector3 Scale
    {
        get => _transform.Scale;
        set
        {
            _transform.Scale = value;
            sceneObject.MarkDirty(SceneDirtyFlags.Transform);
        }
    }

    [InputNumber(Label = "Rotation")]
    public Vector3 EulerRotation
    {
        get => RotationMath.QuaternionToEulerDegrees(in _transform.Rotation);
        set
        {
            _transform.Rotation = RotationMath.EulerDegreesToQuaternion(value);
            sceneObject.MarkDirty(SceneDirtyFlags.Transform);
        }
    }

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

    public void SetBounds(in BoundingAxisBox bounds)
    {
        _bounds = bounds;
        sceneObject.MarkDirty(SceneDirtyFlags.Transform);
    }
    
    internal void CommitTransform(ReadOnlySpan<RenderBlueprintInstance> instances)
    {
        var worldBounds = BoundingBox.Infinite;

        GetTransformMatrix(out var rootMatrix);
        foreach (var instance in instances)
        {
            instance.ApplyTransform(in rootMatrix);
            worldBounds.Expand(in instance.GetWorldBounds());
        }

        SetBounds(worldBounds.ToAxisBox());
    }

}