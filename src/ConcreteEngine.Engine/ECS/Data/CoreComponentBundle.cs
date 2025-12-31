using System.Runtime.InteropServices;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS.Data;

[StructLayout(LayoutKind.Sequential)]
public struct CoreComponentBundle(in SourceComponent source, in RenderTransform transform, in BoxComponent box)
{
    public SourceComponent Source = source;
    public RenderTransform Transform = transform;
    public BoxComponent Box = box;
}