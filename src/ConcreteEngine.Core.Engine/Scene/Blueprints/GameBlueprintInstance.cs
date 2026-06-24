using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.ECS;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class GameBlueprintInstance(SceneObject owner)
{
    protected readonly SceneObject Owner = owner;

    public bool IsDirty { get; private set; } = true;

    internal readonly List<GameEntityId> GameEntityIds = [];

    public abstract GameBlueprint GetBlueprint();
    public string DisplayName => GetBlueprint().DisplayName;
    public int EntityCount => GameEntityIds.Count;

    public ReadOnlySpan<GameEntityId> GetGameEntities() => CollectionsMarshal.AsSpan(GameEntityIds);

    internal void MarkDirty(SceneDirtyFlags flag)
    {
        IsDirty = true;
        Owner.MarkDirty(flag);
    }

    internal void Commit()
    {
        IsDirty = false;
        OnCommit();
    }

    internal abstract void OnCreate();
    protected virtual void OnCommit() { }
}