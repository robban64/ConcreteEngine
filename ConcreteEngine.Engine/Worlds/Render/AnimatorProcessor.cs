using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class AnimatorProcessor
{
    //private readonly Matrix4x4[] _animationGlobals = new Matrix4x4[64];
   // private readonly Matrix4x4[] _animationFinal = new Matrix4x4[64];

    //private readonly WorldEntities _entities;
    private readonly MeshTable _meshTable;


    public AnimatorProcessor(MeshTable meshTable)
    {
        _meshTable = meshTable;

    }


    [SkipLocalsInit]
    public void ProcessAnimations(float deltaTime, WorldEntities entities, DrawCommandBuffer buffer,
        RenderFrameContext ctx)
    {
        //var submitView = _meshTable.GetBoneUploadPayload();
        DrawAnimationUniform finalResult;
        Span<Matrix4x4> globals = stackalloc Matrix4x4[64];

        globals.Fill(Matrix4x4.Identity);
        new AnimationUniformWriter(ref finalResult).FillIdentity(new Range32(0, 64));

        int uboIndex = 0;
        int idx = 0;
        foreach (var query in entities.Query<AnimationComponent>())
        {
            ref var component = ref query.Component;
            
            var modelAnimation = _meshTable.GetAnimationFor(component.Model);
            var animation = modelAnimation.AnimationDataSpan[component.ClipIndex];

            component.Duration = animation.Duration;
            component.Speed = animation.TicksPerSecond;
            float time = component.AdvanceTime(deltaTime);

            var boneByIndex = animation.BoneTracksMap;
            var boneTransforms = modelAnimation.BoneTransforms;
            var nodeTransforms = modelAnimation.NodeTransforms;
            var parentIndices = modelAnimation.ParentIndices;

            var finalWriter = new AnimationUniformWriter(ref finalResult)
            {
                Slot = ref uboIndex
            };

            int boneLength = parentIndices.Length;

            if ((uint)boneLength > globals.Length || (uint)boneLength > finalWriter.Matrices.Length)
            {
                throw new IndexOutOfRangeException();
            }

            //ref var g0 = ref MemoryMarshal.GetReference(globals);
            var invMatrix = modelAnimation.InverseRootTransform;
            for (int i = 0; i < boneLength; i++)
            {
                Matrix4x4 workingMat;
                if (!boneByIndex.TryGetValue(i, out var track))
                {
                    workingMat = nodeTransforms[i];
                }
                else
                {
                    var pos = LerpVector(track.Translations, track.TranslationTimes, time, default);
                    var rot = LerpQuaternion(track.Rotations, track.RotationTimes, time);
                    MatrixMath.CreateModelMatrix(in pos, Vector3.One, in rot, out workingMat);
                    //poseTransform.Scale = LerpVector(track.Scales, track.ScaleTimes, t, Vector3.One);
                    //local = Matrix4x4.CreateFromQuaternion(poseTransform.Rotation) *Matrix4x4.CreateTranslation(poseTransform.Translation);
                }

                var finals = finalWriter.Matrices;
                ref var currentGlobal = ref globals[i];

                int p = parentIndices[i];
                if (p >= 0)
                    MatrixMath.WriteMultiplyAffine(ref currentGlobal, in workingMat, in globals[p]);
                else
                    currentGlobal = workingMat;

                MatrixMath.WriteMultiplyAffine(ref workingMat, in boneTransforms[i], in currentGlobal);
                MatrixMath.WriteMultiplyAffine(ref finals[i], in workingMat, in invMatrix);
                //finals[i] = boneTransforms[i] * globals[i] * invMatrix;
            }

            int noneBoneLength = 64 - boneLength;
            finalWriter.FillIdentity(new Range32(boneLength, noneBoneLength));
            ctx.GetByEntityId(query.Entity).AnimatedSlot = (short)finalWriter.Slot;

            buffer.SubmitSingleAnimation(finalWriter);
            idx++;
        }
    }

    private static Vector3 LerpVector(ReadOnlySpan<Vector3> values, ReadOnlySpan<float> times, float time, Vector3 fallback)
    {
        if (times.Length == 0) return fallback;
        if (times.Length == 1 || time <= times[0]) return values[0];
        if (time >= times[^1]) return values[^1];

        var i = 0;
        while (i < times.Length - 1 && time >= times[i + 1]) i++;

        var f = (time - times[i]) / (times[i + 1] - times[i]);
        return Vector3.Lerp(values[i], values[i + 1], f);
    }

    private static Quaternion LerpQuaternion(ReadOnlySpan<Quaternion> values, ReadOnlySpan<float> times, float time)
    {
        if (times.Length == 0) return Quaternion.Identity;
        if (times.Length == 1 || time <= times[0]) return values[0];
        if (time >= times[^1]) return values[^1];

        var i = 0;
        while (i < times.Length - 1 && time >= times[i + 1]) i++;

        var f = (time - times[i]) / (times[i + 1] - times[i]);
        return Quaternion.Slerp(values[i], values[i + 1], f);
    }
}