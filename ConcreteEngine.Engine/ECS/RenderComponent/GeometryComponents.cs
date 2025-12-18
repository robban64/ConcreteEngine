using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct BoxComponent(in BoundingBox bounds) : IRenderComponent<BoxComponent>
{
    public BoundingBox Bounds = bounds;
    public static implicit operator BoundingBox(BoxComponent c) => c.Bounds;
    public static implicit operator BoxComponent(BoundingBox c) => new(in c);
}