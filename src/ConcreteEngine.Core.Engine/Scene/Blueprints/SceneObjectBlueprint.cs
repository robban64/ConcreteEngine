using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class SceneObjectBlueprint
{
    public string DisplayName = string.Empty;
    public Guid GId = Guid.NewGuid();
    public bool IsDirty;

    public abstract ReadOnlySpan<BlueprintInstance> GetPlainInstances();

}

public abstract class SceneObjectBlueprint<T> : SceneObjectBlueprint where T : BlueprintInstance
{
    private readonly List<T> _instances = [];
    public ReadOnlySpan<T> GetInstanceSpan() => CollectionsMarshal.AsSpan(_instances);
    public override ReadOnlySpan<BlueprintInstance> GetPlainInstances() => GetInstanceSpan();
    
    public void AddInstance(T instance) 
    {
        if(_instances.Contains(instance)) return;
        _instances.Add(instance);
    }
    
    public void RemoveInstance(T instance)
    {
        _instances.Remove(instance);
    }

    private void NotifyChanges()
    {
        foreach (var instance in GetInstanceSpan())
        {
            instance.MarkDirty(SceneDirtyFlags.Blueprint);
        }
    }
}
