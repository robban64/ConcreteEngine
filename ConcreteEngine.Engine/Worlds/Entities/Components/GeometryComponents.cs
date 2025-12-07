using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

[StructLayout(LayoutKind.Sequential)]
public struct BoxComponent(in BoundingBox bounds)
{
    public BoundingBox Bounds = bounds;
    public static implicit operator BoundingBox(BoxComponent c) => c.Bounds;
    public static implicit operator BoxComponent(BoundingBox c) => new(in c);

    //public static ref BoundingBox UnsafeAs(ref BoxComponent box) => ref Unsafe.As<BoxComponent, BoundingBox>(ref box);
}