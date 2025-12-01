#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Render.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

internal static class AnimatorProcessor
{

    [SkipLocalsInit]
    public static void Execute(float deltaTime, AnimationTable animationTable, DrawCommandBuffer buffer, DrawEntityContext ctx)
    {
        const int boneCap = RenderLimits.BoneCapacity;
        Span<Matrix4x4> globals = stackalloc Matrix4x4[boneCap];
        globals.Fill(Matrix4x4.Identity);

        var tableData = animationTable.GetDataView();
        var uploader = buffer.GetSkinningUploaderCtx();

        var idx = 0;
        foreach (var query in WorldEntities.Query<AnimationComponent>())
        {
            ref var component = ref query.Component;
            var time = component.AdvanceTime(deltaTime);

            var view = tableData.GetModelView(component.Animation, out var invTransform);
            var clipTrack = view.GetClip(0);

            var len = view.BoneOffsetMatrixSpan.Length;
            if ((uint)len > boneCap) 
            {
                throw new IndexOutOfRangeException("BoneCount exceeds capacity.");
            }

            var finals = uploader.WriteBoneSpan();
            Matrix4x4 result = default;
            for (var i = 0; i < len; i++)
            {
                ref readonly var track = ref clipTrack[i];

                var local = track.IsEmpty
                    ? view.NodeTransformSpan[i]
                    : SampleKeyFrame(track.Positions, track.Rotations, time);

                var p = view.ParentIndexSpan[i];
                if (p >= 0)
                    MatrixMath.WriteMultiplyAffine(ref globals[i], in local, in globals[p]);
                else
                    globals[i] = local;

                MatrixMath.WriteMultiplyAffine(ref result, in view.BoneOffsetMatrixSpan[i], in globals[i]);
                MatrixMath.WriteMultiplyAffine(ref finals[i], in result, in invTransform);
                //finals[i] = boneTransforms[i] * globals[i] * invMatrix;
            }

            ref var entitySource = ref ctx.GetByEntityId(query.Entity);
            if (entitySource.Source.AnimatedSlot == 0)
            {
                int noneBoneLength = boneCap - len;
                finals.Slice(len, noneBoneLength).Fill(Matrix4x4.Identity);
                entitySource.SetAnimationSlot((ushort)(idx + 1));
            }

            idx++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 SampleKeyFrame(ReadOnlySpan<KeyFrameVec3> pos, ReadOnlySpan<KeyFrameQuat> rot, float time)
    {
        var translation = pos.Length == 1 ? pos[0].Value : SampleVector(pos, time);
        var rotation = rot.Length == 1 ? rot[0].Value : SampleQuaternion(rot, time);

        MatrixMath.CreateModelMatrix(in translation, Vector3.One, in rotation, out var localMatrix);
        return localMatrix;

        static Vector3 SampleVector(ReadOnlySpan<KeyFrameVec3> values, float time)
        {
            int index = FindIndex(values, time);
            ref readonly var k1 = ref values[index];
            ref readonly var k2 = ref values[index + 1];

            float factor = (time - k1.Time) / (k2.Time - k1.Time);
            return Vector3.Lerp(k1.Value, k2.Value, factor);
        }

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