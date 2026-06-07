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
    public const int DefaultCapacity = 512;
    
    private static int _unnamedCounter;
    private static int _nameTick = 1;
    public static SceneStore Instance { get; private set; } = null!;

    private readonly SlotArray<SceneObject> _sceneObjects = new(DefaultCapacity);

    private readonly List<Handle32<SceneObject>>[] _byKind =
        new List<Handle32<SceneObject>>[EnumCache<SceneObjectKind>.Count];

    private readonly Dictionary<string, Handle32<SceneObject>> _byName = new(DefaultCapacity);

    internal SceneStore()
    {
        if(Instance != null) throw new InvalidOperationException("SceneStore already initialized");

        for (int i = 0; i < _byKind.Length; i++)
        {
            var cap = (SceneObjectKind)i == SceneObjectKind.Model ? DefaultCapacity : 32;
            _byKind[i] = new List<Handle32<SceneObject>>(cap);
        }

        _sceneObjects.OnResize = static (oldSize, newSize) =>
            Logger.Log(StringLogEvent.MakeResize(LogScope.Assets, nameof(SceneStore), oldSize, newSize));

        Instance = this;
    }

    public int ActiveCount => _sceneObjects.ActiveCount;

    public int Capacity => _sceneObjects.Capacity;
    
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetCountBy(SceneObjectKind kind) => _byKind[(int)kind].Count;

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


    //
    
    public SceneObject Create(SceneObjectTemplate bp)
    {
        var id = new SceneObjectId(_sceneObjects.AllocateNext() + 1, 1);

        var name = bp.Name;
        if (string.IsNullOrEmpty(name))
            name = $"Unnamed({_unnamedCounter++})";

        if (!_byName.TryAdd(name, id))
            _byName.Add(MakeName(bp.Name), id);

        var sceneObject = _sceneObjects[id.Index()] = BlueprintFactory.BuildSceneObject(id, bp);

        _byKind[(int)sceneObject.Kind].Add(id);
        sceneObject.Attach();
        
        return sceneObject;
    }
    
    private string MakeName(string baseName)
    {
        var ticks = 0;
        var name = $"{baseName}-{_nameTick++}";
        while (_byName.ContainsKey(name))
        {
            name = $"{baseName}-{_nameTick++}";
            if(ticks++ > 100) Throwers.InvalidOperation($"Too many retries for {name}");
        }

        return name;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ActiveObjectEnumerator<SceneObject> GetEnumerator() => _sceneObjects.GetEnumerator();
}