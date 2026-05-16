using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class SceneStore : ISceneObjectNotifier
{
    private const int DefaultCapacity = 512;

    private readonly SlotArray<SceneObject> _sceneObjects = new(DefaultCapacity);

    private readonly List<SceneObjectId>[] _byKind = new List<SceneObjectId>[EnumCache<SceneObjectKind>.Count];

    private readonly Dictionary<string, SceneObjectId> _byName = new(DefaultCapacity);
    
    private readonly BlueprintFactory _factory;
    
    internal readonly HashSet<int> DirtyIds = new(DefaultCapacity);

    internal SceneStore(BlueprintFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        for (int i = 0; i < _byKind.Length; i++)
        {
            var cap = (SceneObjectKind)i == SceneObjectKind.Model ? DefaultCapacity : 32;
            _byKind[i] = new List<SceneObjectId>(cap);
        }

        _factory = factory;
    }

    public int ActiveCount => _sceneObjects.ActiveCount;
    public int Capacity => _sceneObjects.Capacity;

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
        if(it?.Id != id) Throwers.InvalidHandle(id);
        return it;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SceneObject GetInternal(int id)
    {
        var index = id - 1;
        return _sceneObjects[index]!;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetIdByName(string name, out SceneObjectId id) => _byName.TryGetValue(name, out id);
    
    public bool TryGet(SceneObjectId id, [NotNullWhen(true)] out SceneObject? sceneObject)
    {
        return _sceneObjects.TryGet(id.Index(), out sceneObject) && sceneObject.Id == id;
    }

    public bool TryGetByName(string name, out SceneObject sceneObject)
    {
        if (_byName.TryGetValue(name, out var id) && TryGet(id, out sceneObject))
            return true;

        sceneObject = null!;
        return false;
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

    public void MarkDirty(SceneObjectId id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Value, nameof(id));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id.Index(), _sceneObjects.Capacity, nameof(id));
        DirtyIds.Add((int)id);
    }

    internal void ClearDirty() => DirtyIds.Clear();

    //

    private static int _unnamedCounter;

    internal SceneObject Create(SceneObjectTemplate bp)
    {
        var id = new SceneObjectId(_sceneObjects.AllocateNext() + 1, 1);

        var name = bp.Name;
        if (string.IsNullOrEmpty(name))
            name = $"Unnamed({_unnamedCounter++})";

        if (!_byName.TryAdd(name, id))
            throw new InvalidOperationException($"SceneObject with name {name} already exists");

        var sceneObject = _sceneObjects[id.Index()] = _factory.BuildSceneObject(id, bp);

        _byKind[(int)sceneObject.Kind].Add(id);

        sceneObject.Attach(this);
        MarkDirty(id);
        return sceneObject;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SlotArray<SceneObject>.Enumerator GetEnumerator() => _sceneObjects.GetEnumerator();

    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_sceneObjects);

    public ref struct Enumerator(ReadOnlySpan<SceneObject?> sceneObjects)
    {
        private readonly ReadOnlySpan<SceneObject?> _sceneObjects = sceneObjects;
        private int _i = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _sceneObjects.Length)
            {
                if (_sceneObjects[_i] != null) return true;
            }
            return false;
        }

        public readonly SceneObject Current => _sceneObjects[_i]!;

        public Enumerator GetEnumerator()
        {
            _i = -1;
            return this;
        }
    }*/
}