using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Primitives;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMaterial = Silk.NET.Assimp.Material;
using static ConcreteEngine.Engine.Assets.Models.Importer.Constants;

namespace ConcreteEngine.Engine.Assets.Models.Importer;

internal sealed class ModelAnimationProcessor(ModelImportDataStore dataStore, ModelImportState state)
{
    public unsafe bool HasAnimationChannels(AssimpScene* scene)
    {
        if (state.BoneCount == 0) return false;

        for (uint i = 0; i < scene->MNumAnimations; i++)
        {
            var anim = scene->MAnimations[i];
            //string animName = anim->MName.AsString;
            int validChannels = 0;

            for (uint c = 0; c < anim->MNumChannels; c++)
            {
                var channel = anim->MChannels[c];
                // valid channel
                if (state.TryGetBoneIndex(channel->MNodeName.AsString, out int index) && index >= 0)
                    validChannels++;
            }

            if (validChannels > 0) return true;
        }

        return false;
    }
    
    public unsafe void ProcessSceneAnimations(AssimpScene* scene)
    {
        if (state.BoneCount > 0)
        {
            Span<int> defaultData = stackalloc int[state.BoneCount];
            defaultData.Fill(-1);
            
            state.PrepareAnimationState((int)scene->MNumAnimations, defaultData);

            BuildSkeletonHierarchy(scene->MRootNode);
        }

        ProcessAnimations(scene);
    }


    public unsafe void ProcessBoneData(AssimpMesh* mesh)
    {
        var writer = dataStore.WriteBones((int)mesh->MNumVertices);
        writer.FillDefaultSkinningData();
        for (var i = 0; i < mesh->MNumBones; i++)
        {
            var boneIndex = 0;
            ref var bone = ref mesh->MBones[i];
            var name = bone->MName.AsString;

            if (state.TryGetBoneIndex(name, out var value))
            {
                boneIndex = value;
            }
            else
            {
                boneIndex = state.BoneCount;
                state.AppendBone(name, boneIndex);
                writer.BoneTransforms[boneIndex] = bone->MOffsetMatrix;
                InvalidOpThrower.ThrowIf(writer.MaxBones > BoneTransformsCapacity, nameof(BoneTransformsCapacity));

            }

            for (var j = 0; j < 4; j++)
            {
                var weight = bone->MWeights[j];
                ref var data = ref writer.SkinningData[(int)weight.MVertexId];
                if (data.GetVertexId(j) < 0)
                {
                    data.Set(j, boneIndex, weight.MWeight);
                    break;
                }
            }
        }
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

            var animationData = new ModelAnimationData(name, duration, ticksPerSecond);

            for (uint c = 0; c < aiAnim->MNumChannels; c++)
            {
                var channel = aiAnim->MChannels[c];
                var boneName = channel->MNodeName.AsString;

                if (!state.TryGetBoneIndex(boneName, out var index))
                {
                    continue;
                }

                var boneTrack = new BoneTrack();

                // Position
                var posKeys = channel->MPositionKeys;
                var posCount = (int)channel->MNumPositionKeys;

                boneTrack.TranslationTimes = new float[posCount];
                boneTrack.Translations = new Vector3[posCount];
                for (var k = 0; k < posCount; k++)
                {
                    boneTrack.TranslationTimes[k] = (float)posKeys[k].MTime;
                    boneTrack.Translations[k] = posKeys[k].MValue;
                }

                // Rotations
                var rotKeys = channel->MRotationKeys;
                var rotCount = (int)channel->MNumRotationKeys;
                boneTrack.RotationTimes = new float[rotCount];
                boneTrack.Rotations = new Quaternion[rotCount];

                for (var k = 0; k < rotCount; k++)
                {
                    boneTrack.RotationTimes[k] = (float)rotKeys[k].MTime;
                    boneTrack.Rotations[k] = rotKeys[k].MValue;
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

                animationData.BoneTracksMap.Add(index, boneTrack);
            }

            state.AppendAnimation(animationData);
        }
    }
}