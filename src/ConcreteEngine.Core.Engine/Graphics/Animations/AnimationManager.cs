using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.ECS.Render.RenderComponent;

namespace ConcreteEngine.Core.Engine.Graphics.Animations;

internal sealed class AnimationManager
{
    internal static readonly AnimationManager Instance = new();

    private readonly SlotArray<AnimationInstance> _animations = new(8);

    private AnimationManager() { }

    public int AnimationCount => _animations.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<AnimationInstance?> GetAnimationSpan() => _animations.AsSpan();

    public AnimationInstance Get(Id16<AnimationInstance> id) => _animations[id.Index]!;

    public void Interpolate(double alpha)
    {
        int slot = 1;
        foreach (var animation in GetEnumerator())
        {
            var count = FilterEntities(slot, animation.GetEntitySpan());
            if (count == 0) continue;

            var time = animation.Interpolate(alpha);
            ++slot;
        }

    }
        
    private static int FilterEntities(int slot, ReadOnlySpan<RenderEntity> entities)
    {
        var count = 0;
        foreach (var query in RenderEcs.Store<SkinningLink>().SparseQuery(entities))
        {
            if (!RenderEcs.Core.IsVisible(query.Entity)) continue;
            query.Component.AnimationSlot = (ushort)slot;
            ++count;
        }

        return count;
    }
    public void AttachEntity(ModelRig rig, RenderEntity entity, Id16<AnimationInstance> animationId = default)
    {
        if (animationId == 0 && TryGetFirstByRig(rig, out var firstEntry))
            animationId = firstEntry.Id;

        if (animationId == 0 || !_animations.TryGet(animationId.Index, out var animation))
        {
            animationId = new Id16<AnimationInstance>(_animations.AllocateNextId() + 1);
            animation = new AnimationInstance(rig, animationId);
            animation.SetClip(0);
            _animations[animationId.Index] = animation;
        }
        else if (rig != animation.Rig)
        {
            Throwers.InvalidArgument(nameof(rig));
        }


        animation.AddEntity(entity);
        RenderEcs.Core.ToggleDrawFlag(entity, EntityDrawFlags.Skinned, true);
        RenderEcs.Store<SkinningLink>().Add(entity, new SkinningLink(animation.Id));
    }

    private bool TryGetFirstByRig(ModelRig rig, out AnimationInstance animation)
    {
        foreach (var a in _animations)
        {
            if (a.Rig == rig)
            {
                animation = a;
                return true;
            }
        }

        animation = null!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ActiveObjectEnumerator<AnimationInstance> GetEnumerator() => new(_animations.AsSpan());
}

public sealed class AnimationInstance : IComparable<AnimationInstance>
{
    public readonly Id16<AnimationInstance> Id;
    public int ActiveClip { get; private set; } = -1;

    public readonly ModelRig Rig;
    
    public double Time;

    public double Duration;
    public double TicksPerSecond;

    private double _prevTime;
    
    private readonly List<RenderEntity> _renderEntities = [];


    internal AnimationInstance(ModelRig rig, Id16<AnimationInstance> animationId)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentOutOfRangeException.ThrowIfZero(animationId.Id, nameof(animationId));
        Rig = rig;
        Id = animationId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<RenderEntity> GetEntitySpan() => CollectionsMarshal.AsSpan(_renderEntities);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceTime(double dt)
    {
        _prevTime = Time;
        Time += dt * TicksPerSecond;
        if (Time > Duration) Time = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Interpolate(double alpha)
    {
        if (Time < _prevTime)
            return double.Lerp(_prevTime, Time + Duration, alpha) % Duration;

        return double.Lerp(_prevTime, Time, alpha);

    }

    public void AddEntity(RenderEntity entity)
    {
        if (_renderEntities.Contains(entity)) Throwers.InvalidArgument(nameof(entity), "Already added");
        _renderEntities.Add(entity);
    }

    public void RemoveEntity(RenderEntity entity) => _renderEntities.Remove(entity);

    public void SetClip(int clipIndex)
    {
        if (ActiveClip == clipIndex) return;
        ActiveClip = clipIndex;

        var clip = Rig.GetClip(clipIndex);
        Duration = clip.Duration;
        TicksPerSecond = clip.TicksPerSecond;
        Time = 0;
        _prevTime = 0;
    }

    public int CompareTo(AnimationInstance? other)
    {
        if (other is null) return 1;
        if (ReferenceEquals(this, other)) return 0;
        return Id.CompareTo(other.Id);
    }
}