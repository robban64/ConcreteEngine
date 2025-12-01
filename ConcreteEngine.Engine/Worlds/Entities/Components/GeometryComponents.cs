using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

public struct BoxComponent(in BoundingBox box)
{
    public BoundingBox Box = box;
    public static implicit operator BoundingBox(BoxComponent c) => c.Box;
}