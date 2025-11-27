#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Assets.Models.Loader;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;
using static ConcreteEngine.Engine.Assets.Models.ImportProcessors.ImportConstants;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.ImportProcessors;

internal sealed class ModelAnimationProcessor(ModelImportDataStore dataStore, ModelLoaderState state)
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
    

    public unsafe void ProcessBoneData(AssimpMesh* mesh, in Matrix4x4 global)
    {
        int vertexCount = (int)mesh->MNumVertices, boneCount = (int)mesh->MNumBones;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(boneCount, BoneTransformsCapacity);

        dataStore.WriteSkinningData(vertexCount, out var skinningData, out var boneTransforms); //ensure capacity for skinningData
        dataStore.FillDefaultSkinningData();
        var slicedSkinned = skinningData.Slice(0, vertexCount);

        for (var i = 0; i < mesh->MNumBones; i++)
        {
            ref var bone = ref mesh->MBones[i];
            var name = bone->MName.AsString;

            if (state.TryGetBoneIndex(name, out var boneIndex))
            {
                var offsetMat = Matrix4x4.Transpose(bone->MOffsetMatrix);
                boneTransforms[boneIndex] = offsetMat;
                WriteWeightAndIndices(bone, boneIndex, slicedSkinned);
            }
            else
            {
                throw new InvalidOperationException();
            }

        }
        
        // sanitize
        SanitizeSkinningData(vertexCount, slicedSkinned);
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

            if ( boneIndex == 0)
            {
                var offset = Matrix4x4.Identity;
                var current = node->MParent;
                while (current != null)
                {
                    offset *= current->MTransformation; 
                    current = current->MParent;
                }

                dataStore.SkeletonRootOffset = offset;
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
    
    
    private static unsafe void WriteWeightAndIndices(Bone* bone, int boneIndex, Span<SkinningData> skinningData)
    {
        /*
for (var j = 0; j < 4; j++)
{
    var weight = bone->MWeights[j];
    ref var data = ref slicedSkinned[(int)weight.MVertexId];
    if (data.GetVertexId(j) < 0)
    {
        data.Set(j, boneIndex, weight.MWeight);
        break;
    }
}*/
        for (uint j = 0; j < bone->MNumWeights; j++)
        {
            var weight = bone->MWeights[j];

            if (weight.MVertexId >= skinningData.Length) continue;

            ref var data = ref skinningData[(int)weight.MVertexId];
            if (data.BoneIndices.X < 0)
            {
                data.BoneIndices.X = boneIndex;
                data.BoneWeights.X = weight.MWeight;
            }
            else if (data.BoneIndices.Y < 0)
            {
                data.BoneIndices.Y = boneIndex;
                data.BoneWeights.Y = weight.MWeight;
            }
            else if (data.BoneIndices.Z < 0)
            {
                data.BoneIndices.Z = boneIndex;
                data.BoneWeights.Z = weight.MWeight;
            }
            else if (data.BoneIndices.W < 0)
            {
                data.BoneIndices.W = boneIndex;
                data.BoneWeights.W = weight.MWeight;
            }
        }
    }
    

    private static void SanitizeSkinningData(int  vertexCount, Span<SkinningData> skinningData)
    {
        for (int i = 0; i < vertexCount; i++)
        {
            ref var data = ref skinningData[i];
            if (data.BoneIndices.X < 0) data.BoneIndices.X = 0;
            if (data.BoneIndices.Y < 0) data.BoneIndices.Y = 0;
            if (data.BoneIndices.Z < 0) data.BoneIndices.Z = 0;
            if (data.BoneIndices.W < 0) data.BoneIndices.W = 0;
        }
    }
}