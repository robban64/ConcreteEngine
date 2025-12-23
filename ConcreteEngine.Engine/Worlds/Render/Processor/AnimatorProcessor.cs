using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Memory;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class AnimatorProcessor
{
    [SkipLocalsInit]
    public static void Execute(DrawCommandBuffer commandBuffer, AnimationTable animationTable)
    {
        const int boneCap = RenderLimits.BoneCapacity;
        Span<Matrix4x4> globals = stackalloc Matrix4x4[boneCap];
        var uploader = commandBuffer.GetSkinningUploaderCtx();
        var dataView = animationTable.GetDataView();
        var byEntityId = new UnsafeSpan<int>(DrawEntityPipeline.ByEntityId);

        foreach (var query in Ecs.Render.Query<RenderAnimationComponent>())
        {
            if (byEntityId.At(query.RenderEntity.Id) <= 0) continue;

            ref readonly var it = ref query.Component;

            var view = dataView.GetModelView(it.Animation, out var invTransform);

            var clip = view.GetClip(it.Clip);
            var writer = uploader.GetWriter();

            ProcessRootBone(it.Time, clip[0].GetTrackView(), view.GetBoneDataPtr(0, out _), in writer, in invTransform, out globals[0]);

            Matrix4x4 skinMatrix = default;
            var len = view.BoneLength;
            for (int i = 1; i < len; i++)
            {
                var boneOffsetNodePtr = view.GetBoneDataPtr(i, out var p);
                ref readonly var offset = ref boneOffsetNodePtr.Item1;
                ref readonly var node = ref boneOffsetNodePtr.Item2;

                SampleTrack(it.Time, clip[i].GetTrackView(), in node, out var local);

                ref var globalCurrent = ref globals[i];
                ref var outputMatrix = ref writer[i].Value;

                MatrixMath.WriteMultiplyAffine(ref globalCurrent, in local, in globals[p]);
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

            ref var outputMatrix = ref writer[0].Value;
            MatrixMath.MultiplyAffine(in offset, in global, out var skinMatrix);
            MatrixMath.WriteMultiplyAffine(ref outputMatrix, in skinMatrix, in invTransform);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SampleTrack(float time, in BoneTrackView track, in Matrix4x4 node, out Matrix4x4 local)
        {
            if (track.Length == 0)
            {
                local = node;
                return;
            }

            var translation = track.Positions.Length == 1
                ? track.Positions[0].Value
                : SampleVector(time, track.Positions);
            var rotation = track.Rotations.Length == 1
                ? track.Rotations[0].Value
                : SampleQuaternion(time, track.Rotations);

            MatrixMath.CreateFixedSizeModelMatrix(in translation, in rotation, out local);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 SampleVector(float time, Span<KeyFrameVec3> values)
    {
        int index = FindIndex(values, time);
        ref readonly var k1 = ref values[index];
        ref readonly var k2 = ref values[index + 1];

        float factor = (time - k1.Time) / (k2.Time - k1.Time);
        return Vector3.Lerp(k1.Value, k2.Value, factor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion SampleQuaternion(float time, Span<KeyFrameQuat> values)
    {
        int index = FindIndex(values, time);

        ref readonly var k1 = ref values[index];
        ref readonly var k2 = ref values[index + 1];

        float factor = (time - k1.Time) / (k2.Time - k1.Time);
        return Quaternion.Slerp(k1.Value, k2.Value, factor);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindIndex<T>(ReadOnlySpan<T> keys, float time) where T : unmanaged, IKeyFrame, IComparable<float>
    {
        if (time >= keys[^1].Time) return keys.Length - 2;
        if (time <= keys[0].Time) return 0;

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