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

    private SceneObject[] _objects = new SceneObject[DefaultCapacity];
    private SceneObjectHandle[] _handles = new SceneObjectHandle[DefaultCapacity];

    private readonly List<SceneObjectId>[] _byKind = new List<SceneObjectId>[EnumCache<SceneObjectKind>.Count];

    private readonly Dictionary<SceneObjectId, Guid> _toGuid = new(DefaultCapacity);
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
    public SceneObject Get(SceneObjectId id) => _objects[_handles[id.Index()].Slot];

    public bool TryGet(SceneObjectId id, out SceneObject sceneObject)
    {
        sceneObject = null!;
        
        var index = id.Index();
        if ((uint)index >= (uint)_handles.Length) return false;
        
        var slot = _handles[index].Slot;
        if ((uint)slot >= (uint)_objects.Length) return false;

        sceneObject = _objects[slot];
        return true;
    }

    public bool TryGetId(string name, out SceneObjectId id) => _byName.TryGetValue(name, out id);
    public bool TryGetGuid(SceneObjectId id, out Guid gid) => _toGuid.TryGetValue(id, out gid);

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<SceneObjectId> GetIdsByKindSpan(SceneObjectKind kind) =>
        CollectionsMarshal.AsSpan(_byKind[(int)kind]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<SceneObject> GetSceneObjectSpan() => _objects.AsSpan(0, _idx);
    
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

        var guid = Guid.NewGuid();
        _toGuid.Add(id, guid);

        var handle = new SceneObjectHandle(id, index, 1);
        _handles[id.Index()] = handle;

        var sceneObject = _objects[index] = _factory.BuildSceneObject(id, bp);
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

        var actual = _objects[id];

        if (actual is null)
            throw new InvalidOperationException($"SceneObject: {id} does not exist");
        if (actual.Id != id)
            throw new InvalidOperationException($"SceneObject: {id} does not match actual: {actual}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureCapacity(int amount)
    {
        var len = _idx + amount;
        if (len >= _objects.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_objects.Length, len);
            Array.Resize(ref _objects, newSize);

            Logger.LogString(LogScope.Engine, $"SceneObject: resized {newSize}", LogLevel.Warn);
        }

        if (len >= _handles.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_handles.Length, len);
            Array.Resize(ref _handles, newSize);

            Logger.LogString(LogScope.World, $"SceneObject Handles: resized {newSize}", LogLevel.Warn);
        }
    }

    private readonly record struct SceneObjectHandle(int SceneObject, int Slot, ushort Gen)
    {
        public SceneObjectHandle(int sceneObject, int slot, int gen) : this(sceneObject, slot, (ushort)gen) { }

        public bool Validate(SceneObjectId e) => e.Id == SceneObject && e.Gen == Gen;

        public static implicit operator SceneObjectId(SceneObjectHandle h) => new(h.SceneObject, h.Gen);
    }

}