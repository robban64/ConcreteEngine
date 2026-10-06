namespace ConcreteEngine.Core.Engine.Render.Components;

public struct DrawInstanced(int instances) : IRenderComponent<DrawInstanced>
{
    public uint Instances = (uint)instances;
}