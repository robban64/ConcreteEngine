using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render;
public readonly ref struct RenderEntityContext(RenderEntity entity, EntityDataStore data)
{
    public readonly RenderEntity Entity = entity;
    public ref DrawSource Source => ref data.GetSource(Entity.Id);
    public ref DrawPolicy Policy => ref data.GetPolicy(Entity.Id);
    public ref BoundingAxisBox WorldBounds => ref data.GetWorldBounds(Entity.Id);
    public ref TransformUniform Transform => ref data.GetTransform(Entity.Id);
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStatus(DrawStatus status)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(Entity.Gen, 0);
        ref var policy = ref Policy;
        policy = policy.WithStatus(status);
    }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleDrawFlag(EntityDrawFlags flag, bool enabled)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(Entity.Gen, 0);
        if (enabled) Source.DrawFlags |= flag;
        else Source.DrawFlags &= ~flag;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetComponent<T>() where T : unmanaged, IRenderComponent<T> 
        => ref RenderEcs.Store<T>().GetUnchecked(Entity.Id);
        
    public bool AddComponent<T>(T t) where T : unmanaged, IRenderComponent<T>
    {
        ArgumentOutOfRangeException.ThrowIfEqual(Entity.Gen, 0);
        return RenderEcs.Store<T>().Add(Entity, in t);
    }

    public bool RemoveComponent<T>() where T : unmanaged, IRenderComponent<T>
    {
        ArgumentOutOfRangeException.ThrowIfEqual(Entity.Gen, 0);
        return RenderEcs.Store<T>().Remove(Entity);
    }
}