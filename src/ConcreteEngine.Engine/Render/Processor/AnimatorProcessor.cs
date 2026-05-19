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

    // Temp
    private readonly Dictionary<int,  int> _processedInstances = new(16);

    private readonly AnimationTable _animations;
    private readonly SkinningBuffer _skinningBuffer;

    public AnimatorProcessor(AnimationTable animations, SkinningBuffer skinningBuffer)
    {
        _globals = NativeArray.Allocate<Matrix4x4>(RenderLimits.BoneCapacity);
        _animations = animations;
        _skinningBuffer = skinningBuffer;
    }

    public void Dispose() => _globals.Dispose();


    public void Execute(in DrawEntityContext ctx)
    {
        UpdateInterpolate(EngineTime.GameAlpha);

        foreach (var query in Ecs.Render.Query<RenderAnimationComponent>())
        {
            var drawItem = ctx.TryGetVisible(query.Entity);
            if (drawItem.Entity.Id == 0) continue;
            var it = query.Component;
            var hash = it.GetHashCode();
            if (_processedInstances.TryGetValue(hash, out var instanceIndex))
            {
                drawItem.Command.AnimationSlot = (ushort)instanceIndex;
                continue;
            }
            
            var writer = _skinningBuffer.NextWriteView(out var slot);
            var skinningContext = _animations.GetSkinningContext(it.AnimationId, it.Clip);
            ExecuteInner(it.Time, skinningContext, writer);
            drawItem.Command.AnimationSlot = slot;
            _processedInstances[hash] = slot;
        }
        _processedInstances.Clear();
    }

    private static void UpdateInterpolate(float alpha)
    {
        var ecs = Ecs.GetRenderStore<RenderAnimationComponent>();
        foreach (var query in Ecs.Game.Query<AnimationComponent, RenderLink>())
        {
            var renderEntity = query.Component2.RenderEntityId;
            if (renderEntity == default) continue;

            var animationRef = ecs.TryGet(renderEntity);
            if (animationRef.IsNull) continue;

            ref readonly var a = ref query.Component1;

            if (a.Time < a.PrevTime)
                animationRef.Value.Time = float.Lerp(a.PrevTime, a.Time + a.Duration, alpha) % a.Duration;
            else
                animationRef.Value.Time = float.Lerp(a.PrevTime, a.Time, alpha);
        }
    }

    private void ExecuteInner(float time, SkinningContext skinningContext, NativeView<Matrix4x4> writer)
    {
        var globals = _globals.Ptr;

        var len = int.Min(skinningContext.ParentIndices.Length, skinningContext.Channels.Length);
        for (var i = 0; i < len; i++)
        {
            ref readonly var clip = ref skinningContext.Channels[i];
            if (clip.MaxLength == 0)
            {
                globals[i] = skinningContext.BindPose[i];
                continue;
            }

            var pos = GetPosition(time, clip);
            var rot = GetRotation(time, clip);
            MatrixMath.CreateFixedSizeModelMatrix(pos, rot, out globals[i]);
        }

        writer[0] = skinningContext.InverseBindPose[0] * globals[0];
        for (var i = 1; i < len; i++)
        {
            var p = skinningContext.ParentIndices[i];
            globals[i] *= globals[p];
            writer[i] = skinningContext.InverseBindPose[i] * globals[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetPosition(float time, in AnimationClipChannel clip)
    {
        var posIndex = GetIndexFactor(time, clip.GetPositionTimes(), out var posFactor);
        return posIndex > 0
            ? Vector3.Lerp(clip.Positions[posIndex], clip.Positions[posIndex + 1], posFactor)
            : clip.Positions[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion GetRotation(float time, in AnimationClipChannel clip)
    {
        var rotIndex = GetIndexFactor(time, clip.GetRotationTimes(), out var rotFactor);
        return rotIndex > 0
            ? Quaternion.Slerp(clip.Rotations[rotIndex], clip.Rotations[rotIndex + 1], rotFactor)
            : clip.Rotations[0];
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