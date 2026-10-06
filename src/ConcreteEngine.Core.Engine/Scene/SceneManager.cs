using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Render;
using ConcreteEngine.Core.Engine.Render.Components;

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
    private readonly List<ISceneListener> _listeners = [];

    internal SceneManager()
    {
        if (Instance != null!) throw new InvalidOperationException("SceneManager already created");
        Instance = this;
        Store = new SceneStore();
        Raycaster = new RayCaster(Store, Camera.Main.Transform);
    }

    public int DirtyCount => _dirtyIds.Count;

    internal void CommitTick()
    {
        if (_dirtyIds.Count == 0) return;
        foreach (var id in CollectionsMarshal.AsSpan(_dirtyIds))
        {
            var sceneObject = Store.GetUnchecked(id);
            if ((sceneObject.Dirty & SceneDirtyFlags.Name) != 0)
                InvokeRenameListener(sceneObject);
            sceneObject.Commit();
        }
    }

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLinked(int e) => RenderWorld.Dense<SceneLink>()[e].IsLinked;
    
    public SceneObject GetByLinkedEntity(int e)
    {
        var id = GetIdByLinkedEntity(e);
        return Store.Get(id);
    }

    public SceneObjectId GetIdByLinkedEntity(int e)
    {
        var sceneLink = RenderWorld.Dense<SceneLink>()[e];
        if (!sceneLink.IsLinked) Throwers.InvalidArgumentHandle(e);
        return sceneLink.SceneId;
    }
    
    
    public void BindSceneHandle(SceneObjectId sceneId, RenderEntity e)
    {
        e.ValidateEntity();
        ref var sceneLink = ref RenderWorld.Dense<SceneLink>()[e];
        if(sceneLink.IsLinked) Throwers.InvalidArgument("RenderEntity already bound to SceneObject");
        sceneLink = new SceneLink(sceneId);
    }

    public void UnbindSceneHandle(RenderEntity e)
    {
        e.ValidateEntity();
        ref var sceneLink = ref RenderWorld.Dense<SceneLink>()[e];
        if(!sceneLink.IsLinked) Throwers.InvalidArgument("RenderEntity not bound to SceneObject");
        sceneLink = default;
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
        if(!sceneObjectId.IsValid) Throwers.InvalidArgument(nameof(sceneObjectId));
        _dirtyIds.TryAddUniqueSorted(sceneObjectId.Id);
    }

    internal void ClearDirty() => _dirtyIds.Clear();
}