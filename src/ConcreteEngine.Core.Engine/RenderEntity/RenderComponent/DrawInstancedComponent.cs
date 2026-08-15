namespace ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;

public struct DrawInstancedComponent(int instances) : IRenderComponent<DrawInstancedComponent>
{
    public uint Instances = (uint)instances;
}