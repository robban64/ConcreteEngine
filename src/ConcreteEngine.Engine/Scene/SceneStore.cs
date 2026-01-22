using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Editor.Diagnostics;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneStore
{
    private const int DefaultCapacity = 512;

    private static int _idx;
    private static int _handleIdx;

    private SceneObject[] _objects = new SceneObject[DefaultCapacity];
    private SceneObjectHandle[] _handles = new SceneObjectHandle[DefaultCapacity];

    private readonly Dictionary<SceneObjectId, Guid> _toGuid = new(DefaultCapacity);
    private readonly Dictionary<string, SceneObjectId> _byName = new(DefaultCapacity);

    private readonly List<SceneObjectId> _dirtyIds = new(8);

    private readonly BlueprintFactory _factory;

    internal SceneStore(BlueprintFactory factory)
    {
        if (_idx > 0 || _handleIdx > 0) throw new InvalidOperationException();
        ArgumentNullException.ThrowIfNull(factory);
        SceneObject.Bind(this);

        _factory = factory;
    }

    //
    public int Count => _idx;

    //
    public SceneObject Get(SceneObjectId id) => _objects[id.Index()];

    public bool TryGetId(string name, out SceneObjectId id) => _byName.TryGetValue(name, out id);
    public bool TryGetGuid(SceneObjectId id, out Guid gid) => _toGuid.TryGetValue(id, out gid);

    //
    internal ReadOnlySpan<SceneObject> GetSceneObjectSpan() => _objects.AsSpan(0, _idx);
    internal ReadOnlySpan<SceneObjectId> GetDirtySpan() => CollectionsMarshal.AsSpan(_dirtyIds);

    //
    internal void MakeDirty(SceneObjectId id)
    {
        if (!_dirtyIds.Contains(id)) _dirtyIds.Add(id);
    }

    internal void ClearDirty() => _dirtyIds.Clear();

    //

    private static int _unnamedCounter;

    internal SceneObject Create(SceneObjectBlueprint bp)
    {
        EnsureCapacity(1);
        var name = bp.Name;

        var index = _idx++;
        var id = new SceneObjectId(_idx, 1);
        if (string.IsNullOrEmpty(name))
            name = $"Unnamed({_unnamedCounter++})";

        if (!_byName.TryAdd(name, id))
            throw new InvalidOperationException($"SceneObject with name {name} already exists");

        var guid = Guid.NewGuid();
        _toGuid.Add(id, guid);

        var handle = new SceneObjectHandle(id, index, 1);
        _handles[_handleIdx++] = handle;
        MakeDirty(handle);

        return _objects[index] = _factory.BuildSceneObject(id, bp);
    }

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

    private void EnsureCapacity(int amount)
    {
        var len = _idx + amount;
        if (len >= _objects.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_objects.Length, len);
            Array.Resize(ref _objects, newSize);

            Logger.LogString(LogScope.World, $"SceneObject: resized {newSize}", LogLevel.Warn);
        }

        len = _handleIdx + amount;
        if (len >= _handles.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_handles.Length, len);
            Array.Resize(ref _handles, newSize);

            Logger.LogString(LogScope.World, $"SceneObject Handles: resized {newSize}", LogLevel.Warn);
        }
    }

    private readonly record struct SceneObjectHandle(int SceneObject, ushort Slot, ushort Gen)
    {
        public SceneObjectHandle(int sceneObject, int slot, int gen) : this(sceneObject, (ushort)slot, (ushort)gen) { }

        public bool Validate(SceneObjectId e) => e.Id == SceneObject && e.Gen == Gen;
        public static implicit operator SceneObjectId(SceneObjectHandle h) => new(h.SceneObject, h.Gen);
    }
}