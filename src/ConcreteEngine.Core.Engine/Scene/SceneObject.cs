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
            if(!string.IsNullOrEmpty(field)) MarkDirty(SceneDirtyFlags.Name);
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

    public SceneTransform Transform { get; }

    private readonly List<RenderBlueprintInstance> _instances = [];

    internal SceneObject(SceneObjectId id, Guid? gid, string name, bool enabled = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Id, nameof(id));
        ArgumentException.ThrowIfNullOrEmpty(name);

        Id = id;
        GId = gid ?? Guid.NewGuid();
        Name = name;
        Enabled = enabled;
        Visible = true;

        Transform = new SceneTransform(this);
        MarkDirty(SceneDirtyFlags.Transform);
        MarkDirty(SceneDirtyFlags.Instance);
    }

    public void SetName(string newName)
    {
        SceneManager.SceneStore.Rename(this, newName);
        Name = newName;
    }

    //
    public int InstanceCount => _instances.Count;

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<RenderBlueprintInstance> GetInstances() => CollectionsMarshal.AsSpan(_instances);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TInstance GetInstance<TInstance>() where TInstance : RenderBlueprintInstance
    {
        foreach (var it in GetInstances())
        {
            if (it is TInstance component) return component;
        }

        Throwers.InvalidOperation($"Cannot find component of type {typeof(TInstance).Name}");
        return null!;
    }

    public bool TryGetInstance<TInstance>(out TInstance instance) where TInstance : RenderBlueprintInstance
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
    internal void AddInstance(RenderBlueprintInstance instance)
    {
        _instances.Add(instance);
        if (instance is ModelInstance) Kind = SceneObjectKind.Model;
        else if (instance is ParticleInstance) Kind = SceneObjectKind.Particle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MarkDirty(SceneDirtyFlags flags)
    {
        if ((Dirty & flags) != 0) return;
        Dirty |= flags;
        SceneManager.Instance.MarkDirty(Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Commit()
    {
        var flag = Dirty;
        if ((flag & SceneDirtyFlags.Visibility) != 0) CommitVisibility();
        if ((flag & SceneDirtyFlags.Transform) != 0) CommitTransform();
        if ((flag & SceneDirtyFlags.Instance) != 0) CommitInstances();
        Dirty = SceneDirtyFlags.None;
    }


    private void CommitVisibility()
    {
        foreach (var it in GetInstances()) it.ToggleVisibility(Visible);
    }
    
    private void CommitTransform()
    {
        Transform.GetTransformMatrix(out var rootMatrix);
        var worldBounds = BoundingBox.Infinite;
        foreach (var instance in GetInstances())
        {
            instance.ApplyTransform(in rootMatrix);
            BoundingBox.Merge(in worldBounds, in instance.GetWorldBounds(), out worldBounds);
        }
        Transform.SetBounds(worldBounds);
    }

    private void CommitInstances()
    {
        foreach (var it in GetInstances())
        {
            if (it.IsDirty) it.Commit();
        }
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