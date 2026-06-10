using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class SceneManager
{
    public static SceneManager Instance { get; private set; } = null!;
    
    public readonly SceneStore Store;
    public readonly Camera Camera;
    public readonly RayCaster Raycaster;
    
    private readonly List<int> _dirtyIds = new(SceneStore.DefaultCapacity);

    internal SceneManager()
    {
        if (Instance != null!) throw new InvalidOperationException("SceneManager already created");
        Instance = this;
        
        Store = new SceneStore();
        Camera = CameraManager.Instance.Camera;
        
        Raycaster = new RayCaster(Store, Camera.Transforms);
    }
    
    public int DirtyCount => _dirtyIds.Count;
    
    internal void CommitTick()
    {
        if(_dirtyIds.Count == 0) return;
        foreach (var id in CollectionsMarshal.AsSpan(_dirtyIds))
        {
            Store.GetInternal(id).Commit();
        }
    }

    public SceneObject SpawnFrom(Model model, in Transform transform)
    {
        return Store.Create(new SceneObjectTemplate(model.Name, in transform)
        {
            Blueprints = [new ModelBlueprint(model)]
        });
    }

    public SceneObject SpawnFrom(Model model, in Transform transform, params ReadOnlySpan<Material> materials)
    {
        return Store.Create(new SceneObjectTemplate(model.Name, in transform)
        {
            Blueprints = [new ModelBlueprint(model, materials)]
        });
    }
    

    public void MarkDirty(SceneObjectId sceneObjectId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sceneObjectId.Value, nameof(sceneObjectId));
        var id = sceneObjectId.Value;
        if (_dirtyIds.Count == 0)
        {
            _dirtyIds.Add(id);
            return;
        }

        var lastId = _dirtyIds[^1];
        if(lastId == id) return;

        if (id > lastId)
        {
            _dirtyIds.Add(id);
            return;
        }

        var existingIndex = SearchMethod.BinarySearch(CollectionsMarshal.AsSpan(_dirtyIds), id);
        if(existingIndex >= 0) return;
        _dirtyIds.Add(id);
        _dirtyIds.Sort();
    }

    internal void ClearDirty() => _dirtyIds.Clear();

}