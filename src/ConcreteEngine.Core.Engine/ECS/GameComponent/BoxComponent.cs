using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.ECS.GameComponent;

public struct BoxComponent(in BoundingBox bounds) : IGameComponent<BoxComponent>
{
    public BoundingBox Bounds = bounds;
    public static implicit operator BoundingBox(BoxComponent c) => c.Bounds;
    public static implicit operator BoxComponent(BoundingBox c) => new(in c);
}