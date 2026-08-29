namespace ConcreteEngine.Core.Engine.EcsRender.RenderComponent;

public struct DrawInstancedComponent(int instances) : IRenderComponent<DrawInstancedComponent>
{
    public uint Instances = (uint)instances;
}