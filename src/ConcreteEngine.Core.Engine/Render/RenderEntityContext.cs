using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public readonly ref struct DrawEntityContext(int entity, DrawSource source)
{
    public readonly int Entity = entity;
    public readonly DrawSource Source = source;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetComponent<T>() where T : unmanaged, IRenderComponent<T> =>
        ref RenderWorld.Store<T>().GetUnchecked(Entity);
}

public readonly ref struct RenderEntityContext(RenderEntity entity, RenderData data)
{
    public readonly RenderEntity Entity = entity;
    public ref DrawSource Source => ref data.GetSource(Entity.Id);
    public ref DrawPolicy Policy => ref data.GetPolicy(Entity.Id);
    public ref BoundingAxisBox WorldBounds => ref data.GetWorldBounds(Entity.Id);
    public ref TransformUniform Transform => ref data.GetTransform(Entity.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStatus(EntityCullStatus status)
    {
        ref var policy = ref Policy;
        policy = policy.WithStatus(status);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleDrawFlag(EntityDrawMask drawMask, bool enabled)
    {
        if (enabled) Source.DrawMask |= drawMask;
        else Source.DrawMask &= ~drawMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetComponent<T>() where T : unmanaged, IRenderComponent<T> => ref RenderWorld.Store<T>().Get(Entity);

    public bool AddComponent<T>(T t) where T : unmanaged, IRenderComponent<T> => RenderWorld.Store<T>().Add(Entity, in t);

    public bool RemoveComponent<T>() where T : unmanaged, IRenderComponent<T> => RenderWorld.Store<T>().Remove(Entity);
}