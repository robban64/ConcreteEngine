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
    public const int BoneCap = AnimationTable.BoneCap;

    private readonly AnimationTable _animationTable;
    private readonly Matrix4x4[] _buffer = new Matrix4x4[64 * 64]; // move later

    public AnimatorProcessor(AnimationTable animationTable)
    {
        _animationTable = animationTable;
    }


    [SkipLocalsInit]
    public void ProcessAnimations(float deltaTime, WorldEntities entities, DrawCommandBuffer buffer,
        RenderFrameContext ctx)
    {
        Span<Matrix4x4> globals = stackalloc Matrix4x4[BoneCap];
        globals.Fill(Matrix4x4.Identity);

        int idx = 0;
        foreach (var query in entities.Query<AnimationComponent>())
        {
            ref var component = ref query.Component;
            float time = component.AdvanceTime(deltaTime);

            var view = _animationTable.GetModelAnimationView(component.Animation);
            int boneLength = view.ParentIndices.Length;
            var clipTrack = view.GetClip(0);

            if ((uint)boneLength > globals.Length ||
                (uint)boneLength > view.BoneTransforms.Length ||
                (uint)boneLength > view.NodeTransforms.Length ||
                (uint)boneLength > view.ParentIndices.Length ||
                (uint)boneLength > clipTrack.Length)
            {
                throw new IndexOutOfRangeException();
            }

            var finals = _buffer.AsSpan(idx * BoneCap, BoneCap);
            Matrix4x4 result = default;
            for (int i = 0; i < boneLength; i++)
            {
                ref readonly var track = ref clipTrack[i];

                var local = track.IsEmpty
                    ? view.NodeTransforms[i]
                    : SampleKeyFrame(track.Translations, track.Rotations, time);

                int p = view.ParentIndices[i];
                if (p >= 0)
                    MatrixMath.WriteMultiplyAffine(ref globals[i], in local, in globals[p]);
                else
                    globals[i] = local;

                MatrixMath.WriteMultiplyAffine(ref result, in view.BoneTransforms[i], in globals[i]);
                MatrixMath.WriteMultiplyAffine(ref finals[i], in result, in view.InvTransform);
                //finals[i] = boneTransforms[i] * globals[i] * invMatrix;
            }

            ref var drawEntity = ref ctx.GetByEntityId(query.Entity);
            if (drawEntity.AnimatedSlot == -1)
            {
                int noneBoneLength = BoneCap - boneLength;
                finals.Slice(boneLength, noneBoneLength).Fill(Matrix4x4.Identity);
                drawEntity.AnimatedSlot = (short)idx;
            }
            idx++;
        }

        buffer.SubmitAnimationData(_buffer.AsSpan(0, idx * BoneCap));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 SampleKeyFrame(ReadOnlySpan<Vector3Key> pos, ReadOnlySpan<QuaternionKey> rot, float time)
    {
        var translation = pos.Length == 1 ? pos[0].Value : SampleVector(pos, time);
        var rotation = rot.Length == 1 ? rot[0].Value : SampleQuaternion(rot, time);

        MatrixMath.CreateModelMatrix(in translation, Vector3.One, in rotation, out var localMatrix);
        return localMatrix;

        static Vector3 SampleVector(ReadOnlySpan<Vector3Key> values, float time)
        {
            int index = FindIndex(values, time);
            ref readonly var k1 = ref values[index];
            ref readonly var k2 = ref values[index + 1];

            float factor = (time - k1.Time) / (k2.Time - k1.Time);
            return Vector3.Lerp(k1.Value, k2.Value, factor);
        }

        static Quaternion SampleQuaternion(ReadOnlySpan<QuaternionKey> values, float time)
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