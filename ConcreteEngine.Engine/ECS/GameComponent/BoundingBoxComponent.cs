using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Engine.ECS.GameComponent;

public struct BoundingBoxComponent(in BoundingBox bounds): IGameComponent<BoundingBoxComponent>
{
    public BoundingBox Bounds = bounds;
    public static implicit operator BoundingBox(BoundingBoxComponent c) => c.Bounds;
    public static implicit operator BoundingBoxComponent(BoundingBox c) => new(in c);
}