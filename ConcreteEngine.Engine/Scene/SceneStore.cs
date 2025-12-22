using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneStore
{
    private const int DefaultCapacity = 128;

    private static int _idx;
    private static int _handleIdx;

    private SceneObject[] _objects = new SceneObject[DefaultCapacity];
    private SceneObjectHandle[] _handles = new SceneObjectHandle[DefaultCapacity];

    private readonly Dictionary<SceneObjectId, Guid> _toGuid = new(DefaultCapacity);
    private readonly Dictionary<string, SceneObjectId> _byName = new(DefaultCapacity);


    internal SceneStore()
    {
        if (_idx > 0 || _handleIdx > 0) throw new InvalidOperationException();
    }

    internal SceneObject Get(SceneObjectId id) => _objects[id.Index()];

    internal SceneObjectId Create(string name)
    {
        EnsureCapacity(1);

        var index = _idx++;
        var id = new SceneObjectId(_idx, 0);
        if (!string.IsNullOrEmpty(name))
        {
            if (!_byName.TryAdd(name, id))
                throw new InvalidOperationException($"SceneObject with name {name} already exists");
        }

        var guid = Guid.NewGuid();
        _toGuid.Add(id, guid);

        _handles[_handleIdx++] = new SceneObjectHandle(id, index, 1);
        _objects[index] = new SceneObject(id, guid, name);

        return id;
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

    private readonly record struct SceneObjectHandle(int SceneObject, int Slot, ushort Gen)
    {
        public bool Validate(SceneObjectId e) => e.Id == SceneObject && e.Gen == Gen;
        public static implicit operator SceneObjectId(SceneObjectHandle h) => new(h.SceneObject, h.Gen);
    }
}