using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.ECS;

namespace ConcreteEngine.Core.Engine.Scene;

public interface ISceneObjectNotifier
{
    void MarkDirty(SceneObject sceneObject);
    void Rename(SceneObject asset, string newName, Action<string> onSuccess);
}

public sealed class SceneObject : IEquatable<SceneObject>, IComparable<SceneObject>
{
    [Flags]
    public enum DirtyFlags : byte
    {
        None = 0,
        Transform = 1 << 0,
        Instance = 1 << 1,
    }

    public SceneObjectId Id { get; }
    public Guid GId { get; }

    public string Name
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            PackedName = StringPacker.PackAscii(value.AsSpan(), true);
        }
    }

    public ulong PackedName { get; private set; }

    public bool Enabled { get; private set; }
    public SceneObjectKind Kind { get; private set; }
    public DirtyFlags Dirty { get; private set; }

    private Transform _transform;
    private BoundingBox _bounds;

    private readonly List<BlueprintInstance> _instances = [];

    private readonly List<RenderEntityId> _renderEntities = [];
    private readonly List<GameEntityId> _gameEntities = [];

    private ISceneObjectNotifier? _notifier;

    internal SceneObject(
        SceneObjectId id,
        Guid gId,
        string name,
        bool enabled,
        in Transform transform,
        in BoundingBox bounds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Id, nameof(id));
        ArgumentOutOfRangeException.ThrowIfEqual(gId, Guid.Empty);
        ArgumentException.ThrowIfNullOrEmpty(name);

        Id = id;
        GId = gId;
        Name = name;
        Enabled = enabled;
        _transform = transform;
        _bounds = bounds;
    }

    public void SetName(string newName)
    {
        if (_notifier is not { } notifier) return;
        notifier.Rename(this, newName, (name) => Name = name);
    }

    //
    public int RenderEntitiesCount => _renderEntities.Count;
    public int GameEntitiesCount => _gameEntities.Count;

    //
    public Vector3 Translation
    {
        get => _transform.Translation;
        set
        {
            _transform.Translation = value;
            MarkDirty(DirtyFlags.Transform);
        }
    }

    public Vector3 Scale
    {
        get => _transform.Scale;
        set
        {
            _transform.Scale = value;
            MarkDirty(DirtyFlags.Transform);
        }
    }

    public Quaternion Rotation
    {
        get => _transform.Rotation;
        set
        {
            _transform.Rotation = value;
            MarkDirty(DirtyFlags.Transform);
        }
    }

    //
    public ref readonly Transform GetTransform() => ref _transform;
    public ref readonly BoundingBox GetBounds() => ref _bounds;

    public void SetTransform(in Transform transform)
    {
        _transform = transform;
        MarkDirty(DirtyFlags.Transform);
    }

    public void SetBounds(in BoundingBox bounds)
    {
        _bounds = bounds;
        //_notifier?.MarkDirty(this);
    }

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<RenderEntityId> GetRenderEntities() => CollectionsMarshal.AsSpan(_renderEntities);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<GameEntityId> GetGameEntities() => CollectionsMarshal.AsSpan(_gameEntities);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<BlueprintInstance> GetInstances() => CollectionsMarshal.AsSpan(_instances);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TInstance GetInstance<TInstance>() where TInstance : BlueprintInstance
    {
        foreach (var it in GetInstances())
        {
            if (it is TInstance component) return component;
        }

        throw new InvalidOperationException($"Cannot find component of type {typeof(TInstance)}");
    }

    public bool TryGetInstance<TInstance>(out TInstance instance) where TInstance : BlueprintInstance
    {
        foreach (var it in GetInstances())
        {
            if (it is TInstance itInstance)
            {
                instance = itInstance;
                return true;
            }
        }

        instance = null!;
        return false;
    }


    //
    internal void Attach(ISceneObjectNotifier notifier)
    {
        _notifier = notifier;
        MarkDirty(DirtyFlags.Transform);
        MarkDirty(DirtyFlags.Instance);
    }

    internal void AddInstance(BlueprintInstance instance)
    {
        _instances.Add(instance);
        _renderEntities.AddRange(instance.GetRenderEntities());
        _gameEntities.AddRange(instance.GetGameEntities());

        foreach (var entity in instance.GetRenderEntities())
            Ecs.SceneLink.BindSceneHandle(entity, Id);

        foreach (var entity in instance.GetGameEntities())
            Ecs.SceneLink.BindSceneHandle(entity, Id);

        if (instance is ModelInstance) Kind = SceneObjectKind.Model;
        else if (instance is ParticleInstance) Kind = SceneObjectKind.Particle;

        instance.Attach(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MarkDirty(DirtyFlags flags)
    {
        Dirty |= flags;
        _notifier?.MarkDirty(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ClearDirty() => Dirty = DirtyFlags.None;

    internal void EnsureCapacity(int renderEcsCapacity, int gameEcsCapacity)
    {
        _renderEntities.EnsureCapacity(renderEcsCapacity);
        _gameEntities.EnsureCapacity(gameEcsCapacity);
    }

    public int CompareTo(SceneObject? other) => other is null ? 1 : Id.CompareTo(other.Id);

    public bool Equals(SceneObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Id.Equals(other.Id) && GId.Equals(other.GId);
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is SceneObject other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, GId);
}

/*
    internal void AddRenderEntity(RenderEntityId entity)
    {
        _renderEntities.Add(entity);
        _notifier?.MarkDirty(this);
    }

    internal void AddRenderEntities(ReadOnlySpan<RenderEntityId> entities)
    {
        _renderEntities.AddRange(entities);
        _notifier?.MarkDirty(this);
    }

    internal void AddGameEntity(GameEntityId entity)
    {
        _gameEntities.Add(entity);
        _notifier?.MarkDirty(this);
    }

    internal void AddGameEntities(ReadOnlySpan<GameEntityId> entities)
    {
        _gameEntities.AddRange(entities);
        _notifier?.MarkDirty(this);
    }
*/