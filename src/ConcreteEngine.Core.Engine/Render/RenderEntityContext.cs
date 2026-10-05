using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Render.Components;
using static ConcreteEngine.Core.Engine.Render.RenderWorld;

namespace ConcreteEngine.Core.Engine.Render;

public readonly ref struct RenderEntityContext(RenderEntity entity)
{
    public readonly RenderEntity Entity = entity;
    public ref DrawSource Source => ref Dense<DrawSource>()[Entity.Id];
    public ref DrawPolicy Policy => ref Dense<DrawPolicy>()[Entity.Id];
    public ref WorldBox WorldBounds => ref Dense<WorldBox>()[Entity.Id];
    public ref Matrix4x4 Transform => ref Unsafe.As<WorldTransform, Matrix4x4>(ref Dense<WorldTransform>()[Entity.Id]);
    public ref Matrix3X4 Normal => ref Unsafe.As<NormalMatrix, Matrix3X4>(ref Dense<NormalMatrix>()[Entity.Id]);


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
    public ref T GetComponent<T>() where T : unmanaged, IRenderComponent<T> => ref Sparse<T>().Get(Entity);

    public bool AddComponent<T>(T t) where T : unmanaged, IRenderComponent<T> => Sparse<T>().Add(Entity, in t);

    public bool RemoveComponent<T>() where T : unmanaged, IRenderComponent<T> => Sparse<T>().Remove(Entity);
}