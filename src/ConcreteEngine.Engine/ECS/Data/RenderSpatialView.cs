using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS.Data;

public ref struct  RenderSpatialView(ref RenderTransform transform, ref BoxComponent box, ref ParentMatrix parent)
{
    public readonly ref RenderTransform Transform = ref transform;
    public readonly ref BoxComponent Box = ref box;
    public readonly ref ParentMatrix Parent = ref parent;
}