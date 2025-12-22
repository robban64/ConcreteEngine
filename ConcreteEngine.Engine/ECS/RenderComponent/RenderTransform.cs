using System.Numerics;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

public struct RenderTransform : IRenderComponent<RenderTransform>
{
    public Transform Transform;

    public RenderTransform(in Transform transform) => Transform = transform;

    public RenderTransform(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
    {
        Transform.Translation = translation;
        Transform.Rotation = rotation;
        Transform.Scale = scale;
    }

    public static readonly RenderTransform Identity = new(default, Vector3.One, Quaternion.Identity);

    public static implicit operator Transform(RenderTransform t) => t.Transform;
    public static implicit operator RenderTransform(Transform d) => new(in d);
}