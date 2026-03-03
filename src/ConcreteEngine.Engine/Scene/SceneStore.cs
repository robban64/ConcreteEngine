using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Editor.Diagnostics;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneStore: ISceneObjectNotifier
{
    private const int DefaultCapacity = 512;

    private int _idx;

    private int[] _indices = new int[DefaultCapacity];
    private SceneObject[] _sceneObjects = new SceneObject[DefaultCapacity];

    private readonly List<SceneObjectId>[] _byKind = new List<SceneObjectId>[EnumCache<SceneObjectKind>.Count];

    private readonly Dictionary<string, SceneObjectId> _byName = new(DefaultCapacity);
    private readonly List<SceneObjectId> _dirtyIds = new(8);


    private readonly BlueprintFactory _factory;

    internal SceneStore(BlueprintFactory factory)
    {
        if (_idx > 0) throw new InvalidOperationException();
        ArgumentNullException.ThrowIfNull(factory);

        for (int i = 0; i < _byKind.Length; i++)
        {
            var cap = ((SceneObjectKind)i) == SceneObjectKind.Model ? DefaultCapacity : 32;
            _byKind[i] = new List<SceneObjectId>(cap);
        }

        _factory = factory;
    }
    

    //
    public int Count => _idx;

    public int GetCountBy(SceneObjectKind kind) => _byKind[(int)kind].Count;

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SceneObject Get(SceneObjectId id) => _sceneObjects[_indices[id.Index()]];

    public bool TryGet(SceneObjectId id, out SceneObject sceneObject)
    {
        sceneObject = null!;
        
        var index = id.Index();
        if ((uint)index >= (uint)_indices.Length) return false;
        
        var slot = _indices[index];
        if ((uint)slot >= (uint)_sceneObjects.Length) return false;

        sceneObject = _sceneObjects[slot];
        return true;
    }

    public bool TryGetIdByName(string name, out SceneObjectId id) => _byName.TryGetValue(name, out id);

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<SceneObjectId> GetIdsByKindSpan(SceneObjectKind kind) =>
        CollectionsMarshal.AsSpan(_byKind[(int)kind]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<SceneObject> GetSceneObjectSpan() => _sceneObjects.AsSpan(0, _idx);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<SceneObjectId> GetDirtySpan() => CollectionsMarshal.AsSpan(_dirtyIds);

    //
    public void MarkDirty(SceneObject sceneObject)
    {
        if (!_dirtyIds.Contains(sceneObject.Id)) _dirtyIds.Add(sceneObject.Id);
    }

    internal void ClearDirty() => _dirtyIds.Clear();

    //

    private static int _unnamedCounter;

    internal SceneObject Create(SceneObjectBlueprint bp)
    {
        EnsureCapacity(1);

        var index = _idx++;
        var id = new SceneObjectId(_idx, 1);

        var name = bp.Name;
        if (string.IsNullOrEmpty(name))
            name = $"Unnamed({_unnamedCounter++})";

        if (!_byName.TryAdd(name, id))
            throw new InvalidOperationException($"SceneObject with name {name} already exists");

        _indices[id.Index()] = index;
        var sceneObject = _sceneObjects[index] = _factory.BuildSceneObject(id, bp);
        
        _byKind[(int)sceneObject.Kind].Add(id);
        
        sceneObject.Attach(this);
        MarkDirty(sceneObject);
        return sceneObject;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ValidateSceneObjectId(SceneObjectId id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Id, nameof(id.Id));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(id.Id, _idx, nameof(id.Id));

        var actual = _sceneObjects[id];

        if (actual is null)
            throw new InvalidOperationException($"SceneObject: {id} does not exist");
        if (actual.Id != id)
            throw new InvalidOperationException($"SceneObject: {id} does not match actual: {actual}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureCapacity(int amount)
    {
        var len = _idx + amount;
        if (len >= _sceneObjects.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_sceneObjects.Length, len);
            Array.Resize(ref _sceneObjects, newSize);

            Logger.LogString(LogScope.Engine, $"SceneObject: resized {newSize}", LogLevel.Warn);
        }

        if (len >= _indices.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_indices.Length, len);
            Array.Resize(ref _indices, newSize);

            Logger.LogString(LogScope.World, $"SceneObject Handles: resized {newSize}", LogLevel.Warn);
        }
    }
/*
    private readonly record struct SceneObjectHandle(int SceneObject, int Slot, ushort Gen)
    {
        public SceneObjectHandle(int sceneObject, int slot, int gen) : this(sceneObject, slot, (ushort)gen) { }

        public bool Validate(SceneObjectId e) => e.Id == SceneObject && e.Gen == Gen;

        public static implicit operator SceneObjectId(SceneObjectHandle h) => new(h.SceneObject, h.Gen);
    }
*/
}