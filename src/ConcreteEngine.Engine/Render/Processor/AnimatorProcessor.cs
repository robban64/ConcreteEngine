using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Engine.Render.Processor;

internal sealed unsafe class AnimatorProcessor : IDisposable
{
    private NativeArray<Matrix4x4> _globals;

    private readonly RenderEntityStore<SkinningComponent> _skinningEcs;
    private readonly AnimationTable _animations;
    private readonly SkinningBuffer _skinningBuffer;

    private readonly List<RenderEntityId> _entityIds = new(8);

    public AnimatorProcessor(AnimationTable animations, SkinningBuffer skinningBuffer)
    {
        _globals = NativeArray.AlignedAllocate<Matrix4x4>(RenderLimits.BoneCapacity, alignment: 16);
        _skinningEcs = Ecs.GetRenderStore<SkinningComponent>();
        _animations = animations;
        _skinningBuffer = skinningBuffer;
    }

    public void Dispose() => _globals.Dispose();


    public void Tag(in DrawEntityContext ctx)
    {
        foreach (var query in _skinningEcs.Query())
        {
            var drawItem = ctx.TryGetVisible(query.Entity);
            if (drawItem.Entity.Id == 0) continue;
            query.Component.AnimationSlot = _skinningBuffer.NextSlot();
            _entityIds.Add(query.Entity);
        }

        foreach (var query in Ecs.Render.Query<SkinLinkComponent>())
        {
            var drawItem = ctx.TryGetVisible(query.Entity);
            if (drawItem.Entity.Id == 0) continue;
            var slot = _skinningEcs.Get(query.Component.EntityId).AnimationSlot;
            drawItem.Command.AnimationSlot = slot;
        }
    }

    public void Execute()
    {
        UpdateInterpolate();
        for (var i = 0; i < _entityIds.Count; i++)
        {
            var entityId = _entityIds[i];
            var it = _skinningEcs.Get(entityId);
            var skinningContext = _animations.GetSkinningContext(it.AnimationId, it.Clip);
            ExecuteInner(it.Time, skinningContext, _skinningBuffer.GetWriteView(it.AnimationSlot));
        }

        _entityIds.Clear();
    }

    private void UpdateInterpolate()
    {
        var alpha = EngineTime.GameAlpha;

        foreach (var query in Ecs.Game.Query<AnimationComponent, RenderLink>())
        {
            var renderEntity = query.Component2.RenderEntityId;
            if (renderEntity == default) continue;

            var animationRef = _skinningEcs.TryGet(renderEntity);
            if (animationRef.IsNull) continue;

            ref readonly var a = ref query.Component1;

            if (a.Time < a.PrevTime)
                animationRef.Value.Time = float.Lerp(a.PrevTime, a.Time + a.Duration, alpha) % a.Duration;
            else
                animationRef.Value.Time = float.Lerp(a.PrevTime, a.Time, alpha);
        }
    }

    private void ExecuteInner(float time, SkinningContext ctx, NativeView<Matrix4x4> writer)
    {
        var globals = _globals.Ptr;

        var len = ctx.ParentIndices.Length;

        for (var i = 0; i < len; i++)
        {
            ref readonly var channel = ref ctx.Channels[i];
            if (channel.MaxLength == 0)
            {
                globals[i] = ctx.BindPose[i];
                continue;
            }

            var posIndex = GetIndexFactor(time, channel.GetPositionTimes(), out var posFactor);
            var rotIndex = GetIndexFactor(time, channel.GetRotationTimes(), out var rotFactor);

            var pos = GetPosition(posIndex, posFactor, channel.Positions);
            var rot = GetRotation(rotIndex, rotFactor, channel.Rotations);

            MatrixMath.CreateFixedSizeModelMatrix(in pos, in rot, out globals[i]);
        }

        MatrixMath.MultiplyAffine(ref writer[0], in ctx.InverseBindPose[0], in globals[0]);
        for (var i = 1; i < len; i++)
        {
            var p = ctx.ParentIndices[i];
            MatrixMath.MultiplyAffine(ref globals[i], in globals[p]);
            MatrixMath.MultiplyAffine(ref writer[i], in ctx.InverseBindPose[i], in globals[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetPosition(int posIndex, float posFactor, ReadOnlySpan<Vector3> positions)
    {
        return posIndex > 0
            ? Vector3.Lerp(positions[posIndex], positions[posIndex + 1], posFactor)
            : positions[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion GetRotation(int rotIndex, float rotFactor, ReadOnlySpan<Quaternion> rotation)
    {
        return rotIndex > 0
            ? Quaternion.Slerp(rotation[rotIndex], rotation[rotIndex + 1], rotFactor)
            : rotation[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndexFactor(float time, UnsafeSpan<float> times, out float factor)
    {
        if (times.Length == 1)
        {
            factor = 0;
            return -1;
        }

        var index = FindIndex(times, time);
        var i0 = times[index];
        var i1 = times[index + 1];
        factor = (time - i0) / (i1 - i0);
        return index;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindIndex(UnsafeSpan<float> keys, float time)
    {
        if (time >= keys[keys.Length - 1]) return keys.Length - 2;
        if (time <= keys[0]) return 0;

        int lo = 0, hi = keys.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int cmp = keys[mid].CompareTo(time);
            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        int idx = hi;
        return int.Clamp(idx, 0, keys.Length - 2);
    }
}