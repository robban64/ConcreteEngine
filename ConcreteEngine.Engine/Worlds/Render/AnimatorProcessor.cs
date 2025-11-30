using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Render.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class AnimatorProcessor
{
    private readonly AnimationTable _animationTable;

    public AnimatorProcessor(AnimationTable animationTable)
    {
        _animationTable = animationTable;
    }


    [SkipLocalsInit]
    public void ProcessAnimations(float deltaTime, WorldEntities entities, DrawCommandBuffer buffer,
        RenderFrameContext ctx)
    {
        const int boneCap = RenderLimits.BoneCapacity;
        Span<Matrix4x4> globals = stackalloc Matrix4x4[boneCap];
        globals.Fill(Matrix4x4.Identity);

        var idx = 0;
        foreach (var query in entities.Query<AnimationComponent>())
        {
            ref var component = ref query.Component;
            var time = component.AdvanceTime(deltaTime);

            var view = _animationTable.GetModelAnimationView(component.Animation);
            var boneLength = view.BoneLength;
            var clipTrack = view.GetClip(0);

            if ((uint)boneLength > globals.Length ||
                (uint)boneLength > view.BoneOffsetMatrixSpan.Length ||
                (uint)boneLength > view.NodeTransformSpan.Length ||
                (uint)boneLength > view.ParentIndexSpan.Length ||
                (uint)boneLength > clipTrack.Length)
            {
                throw new IndexOutOfRangeException();
            }

            var finals = buffer.WriteBoneSpan();
            Matrix4x4 result = default;
            for (var i = 0; i < boneLength; i++)
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
                MatrixMath.WriteMultiplyAffine(ref finals[i], in result, in view.InvTransform);
                //finals[i] = boneTransforms[i] * globals[i] * invMatrix;
            }

            ref var drawEntity = ref ctx.GetByEntityId(query.Entity);
            if (drawEntity.AnimatedSlot == -1)
            {
                int noneBoneLength = boneCap - boneLength;
                finals.Slice(boneLength, noneBoneLength).Fill(Matrix4x4.Identity);
                drawEntity.AnimatedSlot = (short)idx;
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