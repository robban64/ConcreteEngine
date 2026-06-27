using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Core.Engine.Scene;

public interface ISceneListener
{
    void OnSceneObjectRenamed(SceneObject asset);
    void OnSceneObjectRemoved(SceneObject sceneObject);
}

public sealed class SceneManager
{
    public static SceneManager Instance { get; private set; } = null!;
    public static SceneStore SceneStore => Instance.Store;

    public readonly SceneStore Store;
    public readonly RayCaster Raycaster;

    private readonly List<int> _dirtyIds = new(SceneStore.DefaultCapacity);
    private readonly List<ISceneListener> _listeners = new();

    internal SceneManager()
    {
        if (Instance != null!) throw new InvalidOperationException("SceneManager already created");
        Instance = this;

        Store = new SceneStore();
        Raycaster = new RayCaster(Store, CameraManager.Instance.Camera.Transform);
    }

    public int DirtyCount => _dirtyIds.Count;

    internal void CommitTick()
    {
        if (_dirtyIds.Count == 0) return;
        foreach (var id in CollectionsMarshal.AsSpan(_dirtyIds))
        {
            var sceneObject = Store.GetInternal(id);
            if ((sceneObject.Dirty & SceneDirtyFlags.Name) != 0)
                InvokeRenameListener(sceneObject);
            sceneObject.Commit();
        }
    }

    private void InvokeRenameListener(SceneObject sceneObject)
    {
        foreach (var listener in _listeners) listener.OnSceneObjectRenamed(sceneObject);
    }

    public SceneObject Spawn(string name, in Transform transform, params ReadOnlySpan<IBlueprint> blueprints)
    {
        var sceneObject = Store.Create(name, null, true, blueprints);
        sceneObject.Transform.SetTransform(in transform);
        return sceneObject;
    }

    public SceneObject SpawnFrom(Model model, in Transform transform, params ReadOnlySpan<Material> materials)
    {
        var sceneObject = Store.Create(model.Name, null, true, new ModelBlueprint(model, materials));
        sceneObject.Transform.SetTransform(in transform);
        return sceneObject;
    }

    public SceneObject SpawnFrom(SceneObjectTemplate template)
    {
        var sceneObject = Store.Create(template.Name, template.GId, template.Enabled, template.Blueprints);
        sceneObject.Transform.SetTransform(in template.Transform);
        return sceneObject;
    }


    internal void MarkDirty(SceneObjectId sceneObjectId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sceneObjectId.Id, nameof(sceneObjectId));
        var id = sceneObjectId.Id;
        if (_dirtyIds.Count == 0)
        {
            _dirtyIds.Add(id);
            return;
        }

        var lastId = _dirtyIds[^1];
        if (lastId == id) return;

        if (id > lastId)
        {
            _dirtyIds.Add(id);
            return;
        }

        var existingIndex = SearchMethod.BinarySearch(CollectionsMarshal.AsSpan(_dirtyIds), id);
        if (existingIndex >= 0) return;
        _dirtyIds.Add(id);
        _dirtyIds.Sort();
    }

    internal void ClearDirty() => _dirtyIds.Clear();
    

}