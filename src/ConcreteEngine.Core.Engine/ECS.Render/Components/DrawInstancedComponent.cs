namespace ConcreteEngine.Core.Engine.ECS.Render.Components;

public struct DrawInstancedComponent(int instances) : IRenderComponent<DrawInstancedComponent>
{
    public uint Instances = (uint)instances;
}