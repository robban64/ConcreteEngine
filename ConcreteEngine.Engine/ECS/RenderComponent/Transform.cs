using System.Numerics;
using ConcreteEngine.Shared.World;

namespace ConcreteEngine.Engine.ECS.RenderComponent;


public struct Transform : IRenderComponent<SelectionComponent>
{
    public TransformData Data;

    public Transform(in TransformData data) => Data = data;

    public Transform(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
    {
        Data.Translation = translation;
        Data.Rotation = rotation;
        Data.Scale = scale;
    }

    public static readonly Transform Identity = new(default, Vector3.One, Quaternion.Identity);
    
    public static implicit operator TransformData(Transform t) => t.Data;
    public static implicit operator Transform(TransformData d) => new(in d);
    
}