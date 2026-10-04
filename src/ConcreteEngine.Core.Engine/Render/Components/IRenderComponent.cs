namespace ConcreteEngine.Core.Engine.Render.Components;

public interface IRenderComponent<T> where T : unmanaged, IRenderComponent<T>;

public interface IRenderComponentListener<T> where T : unmanaged, IRenderComponent<T>
{
    void ComponentAdded(RenderEntity entity, ref T component);
    void ComponentRemoved(RenderEntity entity, ref T component);
}