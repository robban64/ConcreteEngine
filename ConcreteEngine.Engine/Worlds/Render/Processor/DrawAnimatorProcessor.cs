using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawAnimatorProcessor
{
    [SkipLocalsInit]
    public static void Execute(RenderEntityHub renderEntities, in DrawEntityContext ctx, in SkinningBufferUploader uploader,
        in AnimationDataView animationView)
    {
        const int boneCap = RenderLimits.BoneCapacity;
        Span<Matrix4x4> globals = stackalloc Matrix4x4[boneCap];
        globals.Fill(Matrix4x4.Identity);

        foreach (var query in renderEntities.Query<AnimationComponent>())
        {
            ref readonly var component = ref query.Component;
            if (!ctx.IsVisible(query.RenderEntity)) continue;
            var view = animationView.GetModelView(component.Animation, out var invTransform);

            var len = view.BoneLength;
            if ((uint)len > boneCap)
                throw new IndexOutOfRangeException("BoneCount exceeds capacity.");

            var finals = uploader.GetWriter();

            Matrix4x4 result = default;
            for (var i = 0; i < len; i++)
            {
                ProcessClip(i, in component, globals, in view);
                MatrixMath.WriteMultiplyAffine(ref result, in view.BoneOffsetMatrixSpan[i], in globals[i]);
                MatrixMath.WriteMultiplyAffine(ref finals[i], in result, in invTransform);
            }
        }

        return;

        static void ProcessClip(int i, in AnimationComponent component, Span<Matrix4x4> globals, in ModelAnimationView view)
        {
            var clip = view.GetClip(component.Clip);
            ref readonly var track = ref clip[i];

            var local = track.IsEmpty
                ? view.NodeTransformSpan[i]
                : SampleKeyFrame(track.Positions, track.Rotations, component.Time);

            var p = view.ParentIndexSpan[i];
            if (p >= 0)
                MatrixMath.WriteMultiplyAffine(ref globals[i], in local, in globals[p]);
            else
                globals[i] = local;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 SampleKeyFrame(ReadOnlySpan<KeyFrameVec3> pos, ReadOnlySpan<KeyFrameQuat> rot, float time)
    {
        var translation = pos.Length == 1 ? pos[0].Value : SampleVector(pos, time);
        var rotation = rot.Length == 1 ? rot[0].Value : SampleQuaternion(rot, time);

        MatrixMath.CreateFixedSizeModelMatrix(in translation, in rotation, out var localMatrix);
        return localMatrix;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3 SampleVector(ReadOnlySpan<KeyFrameVec3> values, float time)
        {
            int index = FindIndex(values, time);
            ref readonly var k1 = ref values[index];
            ref readonly var k2 = ref values[index + 1];

            float factor = (time - k1.Time) / (k2.Time - k1.Time);
            return Vector3.Lerp(k1.Value, k2.Value, factor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Quaternion SampleQuaternion(ReadOnlySpan<KeyFrameQuat> values, float time)
        {
            int index = FindIndex(values, time);
            ref readonly var k1 = ref values[index];
            ref readonly var k2 = ref values[index + 1];

            float factor = (time - k1.Time) / (k2.Time - k1.Time);
            return Quaternion.Slerp(k1.Value, k2.Value, factor);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindIndex<T>(ReadOnlySpan<T> keys, float time) where T : struct, IKeyFrame
    {
        if (time >= keys[^1].Time) return keys.Length - 2;
        if (time <= keys[0].Time) return 0;

        int low = 0;
        int high = keys.Length - 1;

        while (low <= high)
        {
            int mid = (low + high) >> 1;

            if (keys[mid].Time < time)
                low = mid + 1;
            else
                high = mid - 1;
        }

        int idx = high;
        return int.Clamp(idx, 0, keys.Length - 2);
    }
}