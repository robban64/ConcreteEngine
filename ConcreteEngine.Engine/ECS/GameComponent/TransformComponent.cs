using System.Numerics;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Engine.ECS.GameComponent;

public struct TransformComponent : IGameComponent<TransformComponent>
{
    public Transform Transform;
    public TransformComponent(in Transform transform) => Transform = transform;

    public TransformComponent(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
    {
        Transform.Translation = translation;
        Transform.Rotation = rotation;
        Transform.Scale = scale;
    }

    public static implicit operator Transform(TransformComponent t) => t.Transform;
    public static implicit operator TransformComponent(Transform d) => new(in d);

}