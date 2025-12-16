using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

internal struct CoreComponent(in RenderSourceComponent renderSource, in Transform transform, in BoxComponent box)
{
    public RenderSourceComponent RenderSource = renderSource;
    public Transform Transform = transform;
    public BoxComponent Box = box;
}