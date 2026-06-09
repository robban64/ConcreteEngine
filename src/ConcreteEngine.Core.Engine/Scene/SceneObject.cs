using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.ECS;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class SceneObject : IEquatable<SceneObject>, IComparable<SceneObject>
{
    public SceneObjectId Id { get; }
    public Guid GId { get; }

    [JsonIgnore]
    public ulong PackedName { get; private set; }

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

    public bool Enabled
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            MarkDirty(SceneDirtyFlags.Enabled);
        }
    }

    public bool Visible
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            MarkDirty(SceneDirtyFlags.Visibility);
        }
    }

    public SceneObjectKind Kind { get; private set; }

    [JsonIgnore]
    public SceneDirtyFlags Dirty { get; private set; }
    
    [JsonIgnore]
    public bool Attached { get; private set; }

    public SceneTransform Transform { get; }

    private readonly List<BlueprintInstance> _instances = [];
    private readonly List<RenderEntityId> _renderEntities = [];
    private readonly List<GameEntityId> _gameEntities = [];
    
    internal SceneObject(
        SceneObjectId id,
        Guid gId,
        string name,
        bool enabled,
        in Transform transform,
        in BoundingBox bounds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Value, nameof(id));
        ArgumentOutOfRangeException.ThrowIfEqual(gId, Guid.Empty);
        ArgumentException.ThrowIfNullOrEmpty(name);

        Id = id;
        GId = gId;
        Name = name;
        Enabled = enabled;
        Visible = true;

        Transform = new SceneTransform(this, in transform, in bounds);
    }

    public void SetName(string newName)
    {
        SceneStore.Instance.Rename(this, newName, (name) => Name = name);
    }

    //
    public int InstanceCount => _instances.Count;
    public int RenderEntitiesCount => _renderEntities.Count;
    public int GameEntitiesCount => _gameEntities.Count;


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

        Throwers.InvalidOperation($"Cannot find component of type {typeof(TInstance).Name}");
        return null!;
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
    internal void Attach()
    {
        Attached = true;
        MarkDirty(SceneDirtyFlags.Transform);
        MarkDirty(SceneDirtyFlags.Instance);
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MarkDirty(SceneDirtyFlags flags)
    {
        if (!Attached || (Dirty & flags) != 0) return;
        Dirty |= flags;
        SceneManager.Instance.MarkDirty(Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ClearDirty() => Dirty = SceneDirtyFlags.None;

    internal void EnsureCapacity(int renderEcsCapacity, int gameEcsCapacity)
    {
        _renderEntities.EnsureCapacity(renderEcsCapacity);
        _gameEntities.EnsureCapacity(gameEcsCapacity);
    }

    public int CompareTo(SceneObject? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.CompareTo(other.Id);
    }

    public bool Equals(SceneObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Id.Equals(other.Id) && GId.Equals(other.GId);
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is SceneObject other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, GId);
}

