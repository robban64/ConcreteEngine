using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics.Maths;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;

namespace ConcreteEngine.Engine.Assets.Models.Loader.AssimpImporter;

internal sealed class AssimpAnimationProcessor(ModelLoaderDataTable dataTable, ModelLoaderState state)
{
    public unsafe void ProcessSceneAnimations(AssimpScene* scene)
    {
        InvalidOpThrower.ThrowIf(state.BoneCount == 0);

        Span<int> defaultData = stackalloc int[state.BoneCount];
        defaultData.Fill(-1);
        state.PrepareAnimationState((int)scene->MNumAnimations, defaultData);

        BuildSkeletonHierarchy(scene->MRootNode);
        ProcessAnimations(scene);
    }


    public unsafe void BuildSkeletonHierarchy(AssimpNode* node)
    {
        var nodeName = node->MName.AsString;

        if (state.TryGetBoneIndex(nodeName, out int boneIndex))
        {
            if (node->MParent != null)
            {
                var parentName = node->MParent->MName.AsString;
                state.UpdateBoneParentIndexOrDefault(parentName, boneIndex);
            }

            if (boneIndex == 0)
            {
                var offset = Matrix4x4.Identity;
                var current = node->MParent;
                while (current != null)
                {
                    MatrixMath.MultiplyAffine(in current->MTransformation, in offset, out offset);
                    current = current->MParent;
                }

                dataTable.SkeletonRootOffset = offset;
            }
        }

        //  check children
        for (uint i = 0; i < node->MNumChildren; i++)
            BuildSkeletonHierarchy(node->MChildren[i]);
    }

    private unsafe void ProcessAnimations(AssimpScene* scene)
    {
        if (scene->MNumAnimations == 0) return;

        var animationLength = (int)scene->MNumAnimations;

        for (uint i = 0; i < animationLength; i++)
        {
            var aiAnim = scene->MAnimations[i];

            var name = aiAnim->MName.AsString;
            var duration = (float)aiAnim->MDuration;
            var ticksPerSecond = (float)(aiAnim->MTicksPerSecond != 0 ? aiAnim->MTicksPerSecond : 25.0f);

            var animationData = new AnimationClip(name, duration, ticksPerSecond);

            for (uint c = 0; c < aiAnim->MNumChannels; c++)
            {
                var channel = aiAnim->MChannels[c];
                var boneName = channel->MNodeName.AsString;

                if (!state.TryGetBoneIndex(boneName, out var index))
                {
                    continue;
                }

                var boneTrack = new AnimationClip.Track();

                // Position
                var posKeys = channel->MPositionKeys;
                var posCount = (int)channel->MNumPositionKeys;

                boneTrack.PositionTimes = new float[posCount];
                boneTrack.Positions = new Vector3[posCount];
                for (var k = 0; k < posCount; k++)
                {
                    boneTrack.PositionTimes[k] = (float)posKeys[k].MTime;
                    boneTrack.Positions[k] = posKeys[k].MValue;
                }

                // Rotations
                var rotKeys = channel->MRotationKeys;
                var rotCount = (int)channel->MNumRotationKeys;
                boneTrack.RotationTimes = new float[rotCount];
                boneTrack.Rotations = new Quaternion[rotCount];

                for (var k = 0; k < rotCount; k++)
                {
                    boneTrack.RotationTimes[k] = (float)rotKeys[k].MTime;
                    boneTrack.Rotations[k] = rotKeys[k].MValue.AsQuaternion;
                }

                // Scales
                var scaleKeys = channel->MScalingKeys;
                var scaleCount = (int)channel->MNumScalingKeys;
                boneTrack.ScaleTimes = new float[rotCount];
                boneTrack.Scales = new Vector3[rotCount];

                for (var k = 0; k < scaleCount; k++)
                {
                    boneTrack.ScaleTimes[k] = (float)scaleKeys[k].MTime;
                    boneTrack.Scales[k] = scaleKeys[k].MValue;
                }

                animationData.Tracks.Add(index, boneTrack);
            }

            state.AppendAnimation(animationData);
        }
    }
}