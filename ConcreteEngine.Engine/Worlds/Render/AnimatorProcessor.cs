using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render;


internal sealed class AnimatorProcessor
{
    //private readonly Matrix4x4[] _animationGlobals = new Matrix4x4[64];
    private readonly Matrix4x4[] _animationFinal = new Matrix4x4[64];

    //private readonly WorldEntities _entities;
    private readonly MeshTable _meshTable;


    public AnimatorProcessor(MeshTable meshTable)
    {
        _meshTable = meshTable;
        
        //_animationGlobals.AsSpan().Fill(Matrix4x4.Identity);
        _animationFinal.AsSpan().Fill(Matrix4x4.Identity);
    }


    public void ProcessAnimations(float deltaTime, WorldEntities entities, DrawCommandBuffer buffer, RenderFrameContext ctx)
    {
        //var submitView = _meshTable.GetBoneUploadPayload();
        var finals = _animationFinal;
        
        Span<Matrix4x4> globals =  stackalloc Matrix4x4[64];
        int animationIdx = 0;
        foreach (var query in entities.Query<AnimationComponent>())
        {
            ref var component = ref query.Component;
            
            var modelAnimation = _meshTable.GetAnimationFor(component.Model);
            var animation = modelAnimation.AnimationDataSpan[component.ClipIndex];
            component.Duration = animation.Duration;
            component.Speed = animation.TicksPerSecond;
            float time = component.AdvanceTime(deltaTime);

            globals.Fill(Matrix4x4.Identity);
            ctx.GetByEntityId(query.Entity).IsAnimated = true;

            ref readonly var invMatrix = ref modelAnimation.InverseRootTransform;
            var boneByIndex = animation.BoneTracksMap;
            var boneCount = boneByIndex.Count;
            var boneTransforms = modelAnimation.BoneTransforms;
            var nodeTransforms = modelAnimation.NodeTransforms;
            var parentIndices = modelAnimation.ParentIndices;

            if ((uint)boneCount > globals.Length || (uint)boneCount > finals.Length ||
                (uint)boneCount > parentIndices.Length)
                throw new IndexOutOfRangeException();


            Matrix4x4 tempMat = default;
            for (int i = 0; i < boneCount; i++)
            {
                if (!boneByIndex.TryGetValue(i, out var track))
                {
                    tempMat = nodeTransforms[i];
                }
                else
                {
                    var pos = LerpVector(track.Translations, track.TranslationTimes, time, default);
                    var rot = LerpQuaternion(track.Rotations, track.RotationTimes, time);
                    MatrixMath.CreateModelMatrix(in pos, Vector3.One, in rot, out tempMat);
                    //poseTransform.Scale = LerpVector(track.Scales, track.ScaleTimes, t, Vector3.One);
                    //local = Matrix4x4.CreateFromQuaternion(poseTransform.Rotation) *Matrix4x4.CreateTranslation(poseTransform.Translation);
                }

                ref var finalRef = ref Unsafe.AsRef(ref finals[i]);
                ref var globalRef = ref Unsafe.AsRef(ref globals[i]);

                int p = parentIndices[i];
                if (p >= 0)
                    MatrixMath.MultiplyAffine(in tempMat, in globals[p], out  globalRef);
                else
                    globalRef = tempMat;

                MatrixMath.MultiplyAffine(in boneTransforms[i], in globalRef, out tempMat);
                MatrixMath.WriteMultiplyAffine(ref finalRef, in tempMat, in invMatrix);
                // Matrix4x4 a = finals[i] = boneTransforms[i] * globals[i] * invMatrix;
                //MatrixMath.MultiplyAffine(in tempMat, in invMatrix, out finals[i]);
            }

            buffer.SubmitSingleAnimation(finals);
        }

       
    }
    
    private static Vector3 LerpVector(ReadOnlySpan<Vector3> values, float[] times, float time, Vector3 fallback)
    {
        if (times.Length == 0) return fallback;
        if (times.Length == 1 || time <= times[0]) return values[0];
        if (time >= times[^1]) return values[^1];

        var i = 0;
        while (i < times.Length - 1 && time >= times[i + 1]) i++;

        var f = (time - times[i]) / (times[i + 1] - times[i]);
        return Vector3.Lerp(values[i], values[i + 1], f);
    }

    private static Quaternion LerpQuaternion(ReadOnlySpan<Quaternion> values, float[] times, float time)
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