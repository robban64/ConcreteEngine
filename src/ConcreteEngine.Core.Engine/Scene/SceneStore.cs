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

    public int Count { get; private set; }

    private SceneObject?[] _sceneObjects = new SceneObject?[DefaultCapacity];

    private readonly List<SceneObjectId>[] _byKind =
        new List<SceneObjectId>[EnumCache<SceneObjectKind>.Count];

    private readonly Dictionary<string, SceneObjectId> _byName = new(DefaultCapacity);
    
    private readonly Stack<int> _free = [];

    internal SceneStore()
    {
        if(Instance != null) throw new InvalidOperationException("SceneStore already initialized");

        for (int i = 0; i < _byKind.Length; i++)
        {
            var cap = (SceneObjectKind)i == SceneObjectKind.Model ? DefaultCapacity : 32;
            _byKind[i] = new List<SceneObjectId>(cap);
        }

        Instance = this;
    }

    public int FreeCount => _free.Count;
    public int ActiveCount => Count - _free.Count;
    public int Capacity => _sceneObjects.Length;
    
    private SceneObjectId AllocateSlot()
    {
        var freeIndex = SlotHelper.NextSlot(_free, Count);
        if (freeIndex >= 0) return new SceneObjectId(freeIndex + 1, 1);

        if (SlotHelper.EnsureCapacity(ref _sceneObjects, Count, 1, out var oldSize))
            Logger.Log(StringLogEvent.MakeResize(LogScope.Assets, nameof(AssetFileRegistry), oldSize, _sceneObjects.Length));

        return new SceneObjectId(++Count, 1);
    }
    
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetCountBy(SceneObjectKind kind) => _byKind[(int)kind].Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(SceneObjectId id)
    {
        var index = id.Index();
        return (uint)index < (uint)_sceneObjects.Length && _sceneObjects[index]?.Id == id;
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
        var index = id.Index();
        if ((uint)index >= (uint)_sceneObjects.Length)
        {
            sceneObject = null;
            return false;
        }
        
        if (_sceneObjects[index] is {} file && file.Id == id)
        {
            sceneObject = file;
            return true;
        }
        
        sceneObject = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetIdByName(string name, out SceneObjectId id)
    {
        if (_byName.TryGetValue(name, out var handle)) id = handle;
        id = default;
        return id.Value > 0;
    }

    public bool TryGetByName(string name, [NotNullWhen(true)] out SceneObject? sceneObject)
    {
        sceneObject = null;
        return _byName.TryGetValue(name, out var id) && TryGet(id, out sceneObject);
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
        var id = AllocateSlot();
        
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
    public ActiveObjectEnumerator<SceneObject> GetEnumerator() => new(_sceneObjects.AsSpan(0, Count));
}