namespace ConcreteEngine.Core.Engine.Render.Components;

public struct DrawInstancedComponent(int instances) : IRenderComponent<DrawInstancedComponent>
{
    public uint Instances = (uint)instances;
}