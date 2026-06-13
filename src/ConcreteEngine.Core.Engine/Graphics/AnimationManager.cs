using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.Graphics;

internal sealed class AnimationManager
{
    internal static readonly AnimationManager Instance = new();

    private readonly SlotArray<AnimationInstance> _animations = new(8);

    private AnimationManager() { }

    public int AnimationCount => _animations.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<AnimationInstance?> GetAnimationSpan() => _animations.AsSpan();

    internal void Simulate(float dt)
    {
        foreach (var it in _animations)
            it.AdvanceTime(dt);
    }

    public void AttachEntity(ModelRig rig, RenderEntityId entity, Id16<AnimationInstance> animationId = default)
    {
        if (animationId == 0 && TryGetFirstByRig(rig, out var firstEntry))
            animationId = firstEntry.Id;

        if (animationId == 0 || !_animations.TryGet(animationId.Index(), out var animation))
        {
            animationId = new Id16<AnimationInstance>(_animations.AllocateNext() + 1);
            animation = new AnimationInstance(rig, animationId);
            animation.SetClip(0);
            _animations[animationId.Index()] = animation;
        }
        else if (rig != animation.Rig)
        {
            Throwers.InvalidArgument(nameof(rig));
        }


        animation.AddEntity(entity);
        Ecs.GetRenderStore<SkinningComponent>().Add(entity, new SkinningComponent(animation.Id));
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
    public short ActiveClip { get; private set; } = -1;

    public readonly ModelRig Rig;
    
    private readonly List<RenderEntityId> _renderEntities = [];
    
    private AnimationTime _animationTime;

    internal AnimationInstance(ModelRig rig, Id16<AnimationInstance> animationId)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentOutOfRangeException.ThrowIfZero(animationId.Value, nameof(animationId));
        Rig = rig;
        Id = animationId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SkinningContext GetSkinningContext() => Rig.GetSkinningContext(ActiveClip);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<RenderEntityId> GetEntitySpan() => CollectionsMarshal.AsSpan(_renderEntities);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceTime(float delta) => _animationTime.AdvanceTime(delta);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Interpolate() => _animationTime.Interpolate(EngineTime.GameAlpha);

    public void AddEntity(RenderEntityId entity)
    {
        if (_renderEntities.Contains(entity)) Throwers.InvalidArgument(nameof(entity), "Already added");
        _renderEntities.Add(entity);
    }
    public void RemoveEntity(RenderEntityId entity) => _renderEntities.Remove(entity);

    public void SetClip(short clipIndex)
    {
        if (ActiveClip == clipIndex) return;
        ActiveClip = clipIndex;

        var clip = Rig.GetClip(clipIndex);
        _animationTime.Duration = clip.Duration;
        _animationTime.TicksPerSecond = clip.TicksPerSecond;
    }

    public int CompareTo(AnimationInstance? other)
    {
        if (other is null) return 1;
        if (ReferenceEquals(this, other)) return 0;
        return Id.CompareTo(other.Id);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct AnimationTime
{
    public float Time;
    public float PrevTime;

    public float Duration;
    public float TicksPerSecond;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AdvanceTime(float deltaTime)
    {
        PrevTime = Time;
        Time += deltaTime * TicksPerSecond;
        if (Time > Duration) Time = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly float Interpolate(float alpha)
    {
        if (Time < PrevTime)
            return float.Lerp(PrevTime, Time + Duration, alpha) % Duration;

        return float.Lerp(PrevTime, Time, alpha);
    }
}