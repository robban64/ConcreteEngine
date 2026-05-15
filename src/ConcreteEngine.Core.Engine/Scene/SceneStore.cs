using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class SceneStore : ISceneObjectNotifier
{
    private const int DefaultCapacity = 512;

    public int Count { get; private set; }

    //private int[] _indices = new int[DefaultCapacity];
    private SceneObject[] _sceneObjects = new SceneObject[DefaultCapacity];

    private readonly List<Handle16<SceneObject>>[] _byKind =
        new List<Handle16<SceneObject>>[EnumCache<SceneObjectKind>.Count];

    private readonly Dictionary<string, Handle16<SceneObject>> _byName = new(DefaultCapacity);

    internal readonly HashSet<Handle16<SceneObject>> DirtyIds = new(DefaultCapacity);

    private readonly BlueprintFactory _factory;

    internal SceneStore(BlueprintFactory factory)
    {
        if (Count > 0) throw new InvalidOperationException();
        ArgumentNullException.ThrowIfNull(factory);

        for (int i = 0; i < _byKind.Length; i++)
        {
            var cap = (SceneObjectKind)i == SceneObjectKind.Model ? DefaultCapacity : 32;
            _byKind[i] = new List<Handle16<SceneObject>>(cap);
        }

        _factory = factory;
    }

    //

    public int GetCountBy(SceneObjectKind kind) => _byKind[(int)kind].Count;

    //

    public bool Has(SceneObjectId id) => TryGet(id, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SceneObject Get(SceneObjectId id) => _sceneObjects[id.Index()];

    public bool TryGet(SceneObjectId id, out SceneObject sceneObject)
    {
        sceneObject = null!;

        var index = id.Index();
        if ((uint)index >= (uint)_sceneObjects.Length) return false;

        sceneObject = _sceneObjects[index];
        return true;
    }

    public bool TryGetIdByName(string name, out SceneObjectId id)
    {
        if (_byName.TryGetValue(name, out var handle))
        {
            id = (SceneObjectId)handle;
            return true;
        }

        id = SceneObjectId.Empty;
        return false;
    }

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<SceneObject> GetSceneObjectSpan() => _sceneObjects.AsSpan(0, Count);

    //
    public void MarkDirty(Handle16<SceneObject> id) => throw new NotImplementedException();

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
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id.Index(), Count, nameof(id));
        DirtyIds.Add(id);
    }

    internal void ClearDirty() => DirtyIds.Clear();

    //

    private static int _unnamedCounter;

    internal SceneObject Create(SceneObjectTemplate bp)
    {
        EnsureCapacity(1);

        var id = new SceneObjectId(++Count, 1);

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureCapacity(int amount)
    {
        var len = Count + amount;
        if (len >= _sceneObjects.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_sceneObjects.Length, len);
            Array.Resize(ref _sceneObjects, newSize);

            Logger.LogString(LogScope.Engine, $"SceneObject: resized {newSize}", LogLevel.Warn);
        }
    }
}