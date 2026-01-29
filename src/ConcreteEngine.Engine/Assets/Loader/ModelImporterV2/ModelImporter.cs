using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Numerics.Primitives;
using ConcreteEngine.Engine.Assets.Loader.AssimpImporter;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpAnimation = Silk.NET.Assimp.Animation;


namespace ConcreteEngine.Engine.Assets.Loader.ModelImporterV2;

internal sealed unsafe partial class ModelImporter
{
    private static AssimpSceneContext Ctx => AssimpSceneContext.Instance;

    private Assimp _assimp;

    internal ModelImporter()
    {
        _assimp = Assimp.GetApi();
    }

    public void ImportModel(string path)
    {
        var scene = _assimp.ImportFile(path, (uint)AssimpUtils.AssimpFlags);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        ProcessScene(scene);

        for (int i = 0; i < scene->MNumMeshes; i++)
            ProcessMeshData(scene->MMeshes[i], Ctx.Model.Meshes[i]);

        for (int i = 0; i < scene->MNumAnimations; i++)
            ProcessAnimation(scene->MAnimations[i]);
    }


    private void ProcessScene(AssimpScene* scene)
    {
        if (_assimp is null) throw new InvalidOperationException(nameof(_assimp));

        Ctx.Model = new ModelData((int)scene->MNumMeshes);

        if (scene->MNumSkeletons > 0 && scene->MNumAnimations > 0)
        {
            var boneCount = (int)scene->MSkeletons[0]->MNumBones;
            var animationCount = (int)scene->MNumAnimations;
            Ctx.Animation = new Animation(boneCount, animationCount);
        }

        _assimp.Matrix4Inverse(&scene->MRootNode->MTransformation);
        //_assimp.TransposeMatrix4(&scene->MRootNode->MTransformation);
        Traverse(scene, scene->MRootNode, -1, Matrix4x4.Identity);
    }

    private static void Traverse(AssimpScene* scene, AssimpNode* node, int parentIndex, in Matrix4x4 parentWorld)
    {
        var local = Matrix4x4.Transpose(node->MTransformation);
        MatrixMath.MultiplyAffine(in local, in parentWorld, out var world);

        var boneIndex = parentIndex;
        if (node->MNumMeshes > 0)
        {
            var meshIndex = (int)node->MMeshes[0];
            var aMesh = scene->MMeshes[meshIndex];
            Ctx.Model.Meshes[meshIndex] = new MeshEntry
            {
                Name = aMesh->MName.AsString,
                Info = new MeshInfo((int)aMesh->MNumVertices, (byte)meshIndex, (byte)aMesh->MMaterialIndex,
                    (byte)aMesh->MNumBones)
            };

            //Ctx.Model.LocalTransforms[meshIndex] = local;
            Ctx.Model.WorldTransforms[meshIndex] = world;
            boneIndex = ProcessNodeBone(node, aMesh, meshIndex, parentIndex, in local);
        }

        for (var i = 0; i < node->MNumChildren; i++)
            Traverse(scene, node->MChildren[i], boneIndex, in world);
    }

    private static int ProcessNodeBone(AssimpNode* node, AssimpMesh* aMesh, int meshIndex, int parentIndex,
        in Matrix4x4 local)
    {
        var bones = aMesh->MNumBones;
        ref readonly var skeleton = ref Ctx.Animation.SkeletonData;
        for (int b = 0; b < bones; b++)
        {
            var aBone = aMesh->MBones[b];
            if (aBone->MNode != node) continue;

            var boneName = node->MName.AsString;
            var boneIndex = Ctx.BoneNameByIndex.Count;
            Ctx.BoneNameByIndex[boneIndex] = boneName;
            Ctx.BoneIndexByMeshBone[(meshIndex, b)] = boneIndex;

            ref var inverseBind = ref skeleton.InverseBindPose[boneIndex];
            if (parentIndex == -1)
                inverseBind = local;
            else
                Matrix4x4.Invert(local, out inverseBind);

            skeleton.BindPose[boneIndex] = local;
            skeleton.ParentIndices[boneIndex] = (byte)parentIndex;
            return boneIndex;
        }

        return parentIndex;
    }

    private static void ProcessMeshData(AssimpMesh* aMesh, MeshEntry meshEntry)
    {
        var meshIndex = meshEntry.Info.MeshIndex;
        if (meshEntry.Info.NumBones == 0)
        {
            var meshSpan = Ctx.Scratchpad.GetMeshSpan(meshIndex);
            WriteIndices(aMesh, meshSpan.Indices);
            WriteVertices(aMesh, meshEntry, meshSpan.Vertices);
        }
        else
        {
            var meshSpan = Ctx.Scratchpad.GetSkinnedMeshSpan(meshIndex);
            WriteIndices(aMesh, meshSpan.Indices);
            WriteVerticesSkinned(aMesh, meshEntry, meshSpan.Vertices);
        }
    }



    private static void ProcessAnimation(AssimpAnimation* aiAnim)
    {
        var name = aiAnim->MName.AsString;
        var duration = (float)aiAnim->MDuration;
        var ticksPerSecond = (float)(aiAnim->MTicksPerSecond != 0 ? aiAnim->MTicksPerSecond : 25.0f);

        var channels = (int)aiAnim->MNumChannels;
        var animationData = new AnimationClip(name, channels, duration, ticksPerSecond);

        for (uint c = 0; c < aiAnim->MNumChannels; c++)
        {
            var channel = aiAnim->MChannels[c];
            /*
            var boneName = channel->MNodeName.AsString;
            if (!state.TryGetBoneIndex(boneName, out var index))
            {
                continue;
            }*/


            // Position
            var posKeys = channel->MPositionKeys;
            var posCount = (int)channel->MNumPositionKeys;

            var rotKeys = channel->MRotationKeys;
            var rotCount = (int)channel->MNumRotationKeys;

            var boneTrack = new AnimationChannel(posCount, rotCount);

            for (var k = 0; k < posCount; k++)
            {
                boneTrack.PositionTimes[k] = (float)posKeys[k].MTime;
                boneTrack.Positions[k] = posKeys[k].MValue;
            }

            for (var k = 0; k < rotCount; k++)
            {
                boneTrack.RotationTimes[k] = (float)rotKeys[k].MTime;
                boneTrack.Rotations[k] = rotKeys[k].MValue.AsQuaternion;
            }

            animationData.Channels[c] = boneTrack;

            /*
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
            */
        }
    }
}