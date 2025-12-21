using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS.Data;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS;

public sealed class RenderEntityHub
{
    public const int DefaultEntityCapacity = 1024;

    public int EntityCount => GenericStore.CoreStore.ActiveCount;
    public RenderEntityCore Core => GenericStore.CoreStore;

    internal RenderEntityHub()
    {
        if (GenericStore.RenderStoreCount > 0)
            throw new InvalidOperationException("WorldEntities already initialized");

        GenericStore.CoreStore = new RenderEntityCore(DefaultEntityCapacity);
        GenericStore.Render<RenderAnimationComponent>.CreateStore(64);
        GenericStore.Render<ParticleComponent>.CreateStore(16);
        GenericStore.Render<SelectionComponent>.CreateStore(16);
        GenericStore.Render<DebugBoundsComponent>.CreateStore(16);
    }


    public RenderEntityId AddEntity(in CoreComponentBundle data) => GenericStore.CoreStore.AddEntity(in data);

    public void AddComponent<T>(RenderEntityId entity, in T component) where T : unmanaged, IRenderComponent<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity.Id));
        GenericStore.Render<T>.Store.Add(entity, component);
    }

    public void RemoveComponent<T>(RenderEntityId entity) where T : unmanaged, IRenderComponent<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity.Id));
        GenericStore.Render<T>.Store.Remove(entity);
    }

    public void EndTick()
    {
    }

    internal RenderQuery<T>.RenderComponentEnumerator Query<T>() where T : unmanaged, IRenderComponent<T> => new();

    internal RenderQuery.RenderEntityEnumerator CoreQuery() => new();

    public RenderEntityStore<T> GetStore<T>() where T : unmanaged, IRenderComponent<T> => GenericStore.Render<T>.Store;
}