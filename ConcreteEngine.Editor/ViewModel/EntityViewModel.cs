using ConcreteEngine.Editor.Data;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.ViewModel;

public sealed class EntityListViewModel
{
    public int SelectedEntityId { get; set; } = 0;
    public List<EntityViewModel> Entities { get; } = new(128);

    public void ResetState()
    {
        SelectedEntityId = 0;
        Entities.Clear();
    }
}

public sealed class EntityViewModel(
    int entityId,
    string name,
    int componentCount)
{
    public int EntityId { get; } = entityId;
    public string Name { get; } = name;
    public int ComponentCount { get; } = componentCount;
}
