using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

[StructLayout(LayoutKind.Sequential)]
public struct CoreComponentBundle(in SourceComponent source, in Transform transform, in BoxComponent box)
{
    public SourceComponent Source = source;
    public Transform Transform = transform;
    public BoxComponent Box = box;
}