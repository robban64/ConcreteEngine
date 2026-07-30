namespace ConcreteEngine.Core.Engine.RenderEntity;

public interface IRenderComponent<T> where T : unmanaged, IRenderComponent<T>;
public interface IRenderComponentListener<T> where T : unmanaged, IRenderComponent<T>
{
    void ComponentAdded(RenderEntityId entity, ref T component);
    void ComponentRemoved(RenderEntityId entity, ref T component);
}