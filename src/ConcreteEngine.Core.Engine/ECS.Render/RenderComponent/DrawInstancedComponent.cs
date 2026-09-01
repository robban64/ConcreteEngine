namespace ConcreteEngine.Core.Engine.ECS.Render.RenderComponent;

public struct DrawInstancedComponent(int instances) : IRenderComponent<DrawInstancedComponent>
{
    public uint Instances = (uint)instances;
}