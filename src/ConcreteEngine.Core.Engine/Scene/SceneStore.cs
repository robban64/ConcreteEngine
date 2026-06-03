using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class SceneStore 
{
    private const int DefaultCapacity = 512;
    
    private static int _unnamedCounter;
    private static int _nameTick = 1;
    public static SceneStore Instance { get; private set; } = null!;

    private readonly SlotArray<SceneObject> _sceneObjects = new(DefaultCapacity);

    private readonly List<Handle32<SceneObject>>[] _byKind =
        new List<Handle32<SceneObject>>[EnumCache<SceneObjectKind>.Count];

    private readonly Dictionary<string, Handle32<SceneObject>> _byName = new(DefaultCapacity);

    private readonly List<int> _dirtyIds = new(DefaultCapacity);

    private readonly BlueprintFactory _factory;

    internal SceneStore(BlueprintFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if(Instance != null) throw new InvalidOperationException("SceneStore already initialized");

        for (int i = 0; i < _byKind.Length; i++)
        {
            var cap = (SceneObjectKind)i == SceneObjectKind.Model ? DefaultCapacity : 32;
            _byKind[i] = new List<Handle32<SceneObject>>(cap);
        }

        _factory = factory;

        _sceneObjects.OnResize = static (oldSize, newSize) =>
            Logger.Log(StringLogEvent.MakeResize(LogScope.Assets, nameof(SceneStore), oldSize, newSize));

        Instance = this;
    }

    public int ActiveCount => _sceneObjects.ActiveCount;
    public int DirtyCount => _dirtyIds.Count;

    public int Capacity => _sceneObjects.Capacity;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<int> GetDirtySpan() => CollectionsMarshal.AsSpan(_dirtyIds);

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetCountBy(SceneObjectKind kind) => _byKind[(int)kind].Count;

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(SceneObjectId id)
    {
        var index = id.Index();
        return (uint)index < (uint)_sceneObjects.Capacity && _sceneObjects[index]?.Id == id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SceneObject Get(SceneObjectId id)
    {
        var it = _sceneObjects[id.Index()];
        if (it?.Id != id) Throwers.InvalidHandle(id);
        return it;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SceneObject GetInternal(int id)
    {
        var index = id - 1;
        return _sceneObjects[index]!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(SceneObjectId id, [NotNullWhen(true)] out SceneObject? sceneObject)
    {
        return _sceneObjects.TryGet(id.Index(), out sceneObject) && sceneObject.Id == id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetIdByName(string name, out SceneObjectId id)
    {
        if (_byName.TryGetValue(name, out var handle)) id = (SceneObjectId)handle;
        id = default;
        return id.Value > 0;
    }

    public bool TryGetByName(string name, [NotNullWhen(true)] out SceneObject? sceneObject)
    {
        sceneObject = null;
        return _byName.TryGetValue(name, out var id) && TryGet((SceneObjectId)id, out sceneObject);
    }

    //
    public void Rename(SceneObject sceneObject, string newName, Action<string> onSuccess)
    {
        if (sceneObject.Name == newName)
            throw new ArgumentException("Rename: Identical name", nameof(newName));
        if (_byName.ContainsKey(newName))
            throw new ArgumentException("Rename: name already exists", nameof(newName));

        _byName.Remove(newName);
        _byName.Add(newName, sceneObject.Id);
        onSuccess(newName);
    }

    public void MarkDirty(SceneObjectId sceneObjectId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sceneObjectId.Value, nameof(sceneObjectId));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(sceneObjectId.Index(), _sceneObjects.Capacity, nameof(sceneObjectId));
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

    //
    
    public SceneObject Create(SceneObjectTemplate bp)
    {
        var id = new SceneObjectId(_sceneObjects.AllocateNext() + 1, 1);

        var name = bp.Name;
        if (string.IsNullOrEmpty(name))
            name = $"Unnamed({_unnamedCounter++})";

        if (!_byName.TryAdd(name, id))
            throw new InvalidOperationException($"SceneObject with name {name} already exists");

        var sceneObject = _sceneObjects[id.Index()] = _factory.BuildSceneObject(id, bp);

        _byKind[(int)sceneObject.Kind].Add(id);

        sceneObject.Attach();
        MarkDirty(id);
        return sceneObject;
    }
    
    
    public SceneObject SpawnFrom(Model model, in Transform transform)
    {
        var materialIndices = model.AssetRefs.MaterialIndices;
        var materials = materialIndices.Length > 0
            ? new AssetId[materialIndices.Length]
            : [MaterialStore.FallbackMaterial.Id];

        if (materials.Length > 1)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                if (!AssetStore.Instance.TryGetByGuid<Material>(materialIndices[i].AssetGId, out var material))
                    material = MaterialStore.FallbackMaterial;

                materials[i] = material.Id;
            }
        }

        var name = $"{model.Name}-{_nameTick++}";
        return Create(new SceneObjectTemplate(name, in transform)
        {
            Blueprints = { new ModelBlueprint(model.Id, materials) }
        });
    }

    public SceneObject SpawnFrom(Model model, in Transform transform, params ReadOnlySpan<AssetId> materialIds)
    {
        var materials = new AssetId[materialIds.Length];
        for (int i = 0; i < materialIds.Length; i++)
        {
            if (!AssetStore.Instance.TryGet<Material>(materialIds[i], out var material))
                material = MaterialStore.FallbackMaterial;

            materials[i] = material.Id;
        }

        var name = $"{model.Name}-{_nameTick++}";
        return Create(new SceneObjectTemplate(name, in transform)
        {
            Blueprints = { new ModelBlueprint(model.Id, materials) }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ActiveObjectEnumerator<SceneObject> GetEnumerator() => _sceneObjects.GetEnumerator();
}