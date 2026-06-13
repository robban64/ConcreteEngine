using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

internal sealed unsafe class AnimationProcessor : IDisposable
{
    private NativeArray<Matrix4x4> _globals;

    private readonly AnimationManager _animations;
    private readonly SkinningBuffer _skinningBuffer;

    private readonly List<GameEntityId> _entityIds = new(8);

    public AnimationProcessor(AnimationManager animations, SkinningBuffer skinningBuffer)
    {
        _globals = NativeArray.AlignedAllocate<Matrix4x4>(RenderLimits.BoneCapacity, alignment: 16);
        _animations = animations;
        _skinningBuffer = skinningBuffer;
    }

    public void Dispose() => _globals.Dispose();
   

    public void Execute()
    {
        _entityIds.Clear();
        ProcessEntities();
        Upload();
    }

    private void ProcessEntities()
    {
        foreach (var animationQuery in Ecs.Game.Query<AnimationComponent>())
        {
            var count = 0;
            var slot = (ushort)(_entityIds.Count + 1);
            foreach (var skinQuery in Ecs.GetRenderStore<SkinningComponent>().VisibilityQuery())
            {
                if(animationQuery.Entity != skinQuery.Component.LinkedAnimationEntity) continue;
                Ecs.RenderCore.GetSource(skinQuery.Entity).AnimationSlot = slot;
                ++count;
            }

            if (count == 0) continue;

            animationQuery.Component.Interpolate(EngineTime.GameAlpha);
            _entityIds.Add(animationQuery.Entity);
            _skinningBuffer.NextSlot();
        }
    }
    

    private void Upload()
    {
        for (var i = 0; i < _entityIds.Count; i++)
        {
            var it = Ecs.GetGameStore<AnimationComponent>().Get(_entityIds[i]);
            var skinningContext = _animations.GetSkinningContext(it.RigId, it.Clip);
            if (skinningContext.Length == 0) continue;
            WriteSkinned(it.InterpolatedTime, in skinningContext, _skinningBuffer.GetWriteView(i+1));
        }
    }

    private void WriteSkinned(float time, in SkinningContext ctx, NativeView<Matrix4x4> writer)
    {
        var globals = _globals.Ptr;
        var length = ctx.Length;
        for (var i = 0; i < length; i++)
        {
            var track = ctx.Tracks.BoneTracks[i];
            if (track.IsEmpty)
            {
                globals[i] = ctx.BindPose[i];
                continue;
            }

            var posFactor = GetIndexFactor(time, track.PositionTimes, out var posIndex);
            var rotFactor = GetIndexFactor(time, track.RotationTimes, out var rotIndex);

            var pos = GetPosition(posIndex, posFactor, track.Positions);
            var rot = GetRotation(rotIndex, rotFactor, track.Rotations);

            MatrixMath.CreateFixedSizeModelMatrix(in pos, in rot, out globals[i]);
        }

        MatrixMath.MultiplyAffine(ref writer[0], in ctx.InverseBindPose[0], in globals[0]);
        for (var i = 1; i < length; i++)
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
    private static float GetIndexFactor(float time, NativeView<float> times, out int index)
    {
        if (times.Length == 1)
        {
            index = -1;
            return 0;
        }

        index = FindIndex(times, time);
        var i0 = times[index];
        var i1 = times[index + 1];
        return (time - i0) / (i1 - i0);
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