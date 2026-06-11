using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Engine.Processor;

internal sealed unsafe class AnimatorProcessor : IDisposable
{
    private NativeArray<Matrix4x4> _globals;

    private readonly RenderEntityStore<SkinningComponent> _skinningEcs;
    private readonly AnimationManager _animations;
    private readonly SkinningBuffer _skinningBuffer;

    private readonly List<RenderEntityId> _entityIds = new(8);

    public AnimatorProcessor(AnimationManager animations, SkinningBuffer skinningBuffer)
    {
        _globals = NativeArray.AlignedAllocate<Matrix4x4>(RenderLimits.BoneCapacity, alignment: 16);
        _skinningEcs = Ecs.GetRenderStore<SkinningComponent>();
        _animations = animations;
        _skinningBuffer = skinningBuffer;
    }

    public void Dispose() => _globals.Dispose();

    private AvgFrameTimer avg;

    public void Execute()
    {
        UpdateInterpolate();
        ProcessRenderEcs();

        avg.BeginSample();
        for (var i = 0; i < _entityIds.Count; i++)
        {
            var entityId = _entityIds[i];
            var it = _skinningEcs.Get(entityId);
            var skinningContext = _animations.GetSkinningContext(it.AnimationId, it.Clip);
            ExecuteInner(it.Time, skinningContext, _skinningBuffer.GetWriteView(it.AnimationSlot));
        }

        if (avg.EndSample() >= 144) avg.ResetAndPrint();

        _entityIds.Clear();
    }

    private void ProcessRenderEcs()
    {
        foreach (var query in _skinningEcs.VisibilityQuery())
        {
            query.Component.AnimationSlot = _skinningBuffer.NextSlot();
            _entityIds.Add(query.Entity);
        }

        foreach (var query in Ecs.GetRenderStore<SkinLinkComponent>().VisibilityQuery())
        {
            var slot = _skinningEcs.Get(query.Component.EntityId).AnimationSlot;
            Ecs.Render.Core.GetSource(query.Entity).AnimationSlot = slot;
        }
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
            var track = ctx.Tracks.BoneTracks[i];
            if (track.IsNull || track.MaxLength == 0)
            {
                globals[i] = ctx.BindPose[i];
                continue;
            }

            var posIndex = GetIndexFactor(time, track.PositionTimes, out var posFactor);
            var rotIndex = GetIndexFactor(time, track.RotationTimes, out var rotFactor);

            var pos = GetPosition(posIndex, posFactor, track.Positions);
            var rot = GetRotation(rotIndex, rotFactor, track.Rotations);

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
    private static Vector3 GetPosition(int posIndex, float posFactor, NativeView<Vector3> positions)
    {
        return posIndex > 0
            ? Vector3.Lerp(positions[posIndex], positions[posIndex + 1], posFactor)
            : positions[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion GetRotation(int rotIndex, float rotFactor, NativeView<Quaternion> rotation)
    {
        return rotIndex > 0
            ? Quaternion.Slerp(rotation[rotIndex], rotation[rotIndex + 1], rotFactor)
            : rotation[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndexFactor(float time, NativeView<float> times, out float factor)
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
    private static int FindIndex(NativeView<float> keys, float time)
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