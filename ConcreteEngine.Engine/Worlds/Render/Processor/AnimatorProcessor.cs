using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Generics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class AnimatorProcessor
{
    private static int _lastLen = 0;

    [SkipLocalsInit]
    public static void Execute(DrawCommandBuffer commandBuffer, AnimationTable animationTable)
    {
        const int boneCap = RenderLimits.BoneCapacity;
        Span<Matrix4x4> globals = stackalloc Matrix4x4[boneCap];

        var uploader = commandBuffer.GetSkinningUploaderCtx();
        var animationView = animationTable.GetDataView();

        var byEntityId = new UnsafeSpan<int>(DrawEntityPipeline.ByEntityId);

        foreach (var query in RenderQuery<RenderAnimationComponent>.Query())
        {
            var entityId = query.RenderEntity.Id;
            if (byEntityId.At(entityId) <= 0) continue;

            var component = query.Component;

            var view = animationView.GetModelView(component.Animation, out var invTransform);
            var clip = view.GetClip(component.Clip);

            var len = _lastLen = view.BoneLength;
            if ((uint)len > boneCap || (uint)len > clip.Length)
                throw new IndexOutOfRangeException("BoneCount exceeds capacity.");

            Matrix4x4 skinMatrix = default;
            var writer = uploader.GetWriter();

            ProcessRootBone(component.Time, clip[0].GetTrackView(), view.GetBoneDataPtr(0, out _),
                new TuplePtr<Matrix4x4, Matrix4x4>(ref globals[0], ref invTransform), in writer);

            for (int i = 1; i < len; i++)
            {
                var boneOffsetNodePtr = view.GetBoneDataPtr(i, out var p);

                ref readonly var offset = ref boneOffsetNodePtr.Item1;
                ref readonly var node = ref boneOffsetNodePtr.Item2;

                SampleTrack(component.Time, clip[i].GetTrackView(), in node, out var local);

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
            TuplePtr<Matrix4x4, Matrix4x4> globalPtr, in SpanSlice<Matrix4x4> writer)
        {
            ref readonly var offset = ref boneOffsetNodePtr.Item1;
            ref readonly var node = ref boneOffsetNodePtr.Item2;

            SampleTrack(time, in track, in node, out globalPtr.Item1);

            ref var outputMatrix = ref writer[0].Value;
            MatrixMath.MultiplyAffine(in offset, in globalPtr.Item1, out var skinMatrix);
            MatrixMath.WriteMultiplyAffine(ref outputMatrix, in skinMatrix, in globalPtr.Item2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SampleTrack(float time, in BoneTrackView track, in Matrix4x4 node, out Matrix4x4 local)
        {
            if (track.Length == 0)
            {
                local = node;
                return;
            }

            SampleKeyFrame(time, in track, out var translation, out var rotation);
            MatrixMath.CreateFixedSizeModelMatrix(in translation, in rotation, out local);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SampleKeyFrame(float time, in BoneTrackView track, out Vector3 translation,
        out Quaternion rotation)
    {
        translation = track.Positions.Length == 1
            ? track.Positions.At(0).Value
            : SampleVector(time, in track.Positions);
        rotation = track.Rotations.Length == 1
            ? track.Rotations.At(0).Value
            : SampleQuaternion(time, in track.Rotations);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 SampleVector(float time, in UnsafeSpan<KeyFrameVec3> values)
    {
        int index = FindIndex(values.Span, time);
        ref readonly var k1 = ref values.At(index);
        ref readonly var k2 = ref values.At(index + 1);

        float factor = (time - k1.Time) / (k2.Time - k1.Time);
        return Vector3.Lerp(k1.Value, k2.Value, factor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion SampleQuaternion(float time, in UnsafeSpan<KeyFrameQuat> values)
    {
        int index = FindIndex(values.Span, time);

        ref readonly var k1 = ref values.At(index);
        ref readonly var k2 = ref values.At(index + 1);

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