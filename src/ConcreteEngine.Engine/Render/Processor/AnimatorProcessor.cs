using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class AnimatorProcessor
{
    private static readonly NativeArray<Matrix4x4> Globals = new(RenderLimits.BoneCapacity);

    [SkipLocalsInit]
    public static void Execute(DrawCommandBuffer commandBuffer, AnimationTable animationTable,
        UnsafeSpan<int> byEntityId)
    {
        var uploader = commandBuffer.GetSkinningUploaderCtx();
        var dataView = animationTable.GetDataView();
        var globals = Globals;

        foreach (var query in Ecs.Render.Query<RenderAnimationComponent>())
        {
            if (byEntityId[query.RenderEntity] == -1) continue;

            var it = query.Component;

            var view = dataView.GetModelView(it.Animation, out var invTransform);

            var clip = view.GetClip(it.Clip);
            var writer = uploader.GetWriter();

            ProcessRootBone(it.Time, clip[0].GetTrackView(), view.GetBoneDataPtr(0, out _), in writer, in invTransform,
                out globals.GetRef());

            Matrix4x4 skinMatrix = default;
            var len = view.BoneLength;
            for (int i = 1; i < len; i++)
            {
                var boneOffsetNodePtr = view.GetBoneDataPtr(i, out var p);
                ref readonly var offset = ref boneOffsetNodePtr.Item1;
                ref readonly var node = ref boneOffsetNodePtr.Item2;

                SampleTrack(it.Time, clip[i].GetTrackView(), in node, out var local);

                ref var outputMatrix = ref writer[i];
                ref var globalCurrent = ref globals.GetRef(i);
                MatrixMath.WriteMultiplyAffine(ref globalCurrent, in local, in globals.GetRef(p));
                MatrixMath.WriteMultiplyAffine(ref skinMatrix, in offset, in globalCurrent);
                MatrixMath.WriteMultiplyAffine(ref outputMatrix, in skinMatrix, in invTransform);
            }
        }

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ProcessRootBone(float time, BoneTrackView track, TuplePtr<Matrix4x4, Matrix4x4> boneOffsetNodePtr,
            in SpanSlice<Matrix4x4> writer, in Matrix4x4 invTransform, out Matrix4x4 global)
        {
            ref readonly var offset = ref boneOffsetNodePtr.Item1;
            ref readonly var node = ref boneOffsetNodePtr.Item2;

            SampleTrack(time, in track, in node, out global);

            ref var outputMatrix = ref writer[0];
            MatrixMath.MultiplyAffine(in offset, in global, out var skinMatrix);
            MatrixMath.WriteMultiplyAffine(ref outputMatrix, in skinMatrix, in invTransform);
        }

        static void SampleTrack(float time, in BoneTrackView track, in Matrix4x4 node, out Matrix4x4 local)
        {
            if (track.Length == 0)
            {
                local = node;
                return;
            }
            
            MatrixMath.CreateFixedSizeModelMatrix(SampleVector(time, track.Positions), SampleQuaternion(time, track.Rotations), out local);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 SampleVector(float time, UnsafeSpan<KeyFrameVec3> values)
    {
        if(values.Length == 1) return values[0].Value;
        int index = FindIndex(values, time);
        ref readonly var k1 = ref values[index];
        ref readonly var k2 = ref values[index + 1];

        float factor = (time - k1.Time) / (k2.Time - k1.Time);
        return Vector3.Lerp(k1.Value, k2.Value, factor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion SampleQuaternion(float time, UnsafeSpan<KeyFrameQuat> values)
    {
        if(values.Length == 1) return values[0].Value;

        int index = FindIndex(values, time);

        ref readonly var k1 = ref values[index];
        ref readonly var k2 = ref values[index + 1];

        float factor = (time - k1.Time) / (k2.Time - k1.Time);
        return Quaternion.Slerp(k1.Value, k2.Value, factor);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindIndex<T>(UnsafeSpan<T> keys, float time) where T : unmanaged, IKeyFrame, IComparable<float>
    {
        if (time >= keys.At(keys.Length - 1).Value.Time) return keys.Length - 2;
        if (time <= keys.At(0).Value.Time) return 0;

        int lo = 0, hi = keys.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int cmp = keys.At(mid).Value.CompareTo(time);
            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        int idx = hi;
        return int.Clamp(idx, 0, keys.Length - 2);
    }
}