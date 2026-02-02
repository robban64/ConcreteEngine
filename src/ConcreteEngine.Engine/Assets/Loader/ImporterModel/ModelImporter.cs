using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpAnimation = Silk.NET.Assimp.Animation;


namespace ConcreteEngine.Engine.Assets.Loader.ImporterModel;

internal sealed unsafe partial class ModelImporter : IDisposable
{
    private static ModelImportContext Ctx => ModelImportContext.Instance;

    private Assimp _assimp;

    internal ModelImporter()
    {
        _assimp = Assimp.GetApi();
    }

    public void Dispose()
    {
        _assimp.Dispose();
        _assimp = null!;
    }


    public void ImportModel(string name, string path, AssetGfxUploader gfxUploader)
    {
        var scene = _assimp.ImportFile(path, (uint)AssimpUtils.AssimpFlags);

        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new InvalidOperationException(error);
        }

        PreProcessScene(scene);
        Begin(name, path, scene);

        Traverse(scene->MRootNode, Matrix4x4.Identity);

        for (var i = 0; i < scene->MNumMeshes; i++)
            ProcessMeshData(scene->MMeshes[i], i, Ctx.Model);

        for (var i = 0; i < scene->MNumAnimations; i++)
            ProcessAnimation(scene->MAnimations[i]);

        ProcessSceneMaterials(scene);

        End(scene, gfxUploader);
    }

    private void Begin(string name, string path, AssimpScene* scene)
    {
        var meshCount = (int)scene->MNumMeshes;
        if (scene->MNumMeshes == 0)
            throw new InvalidOperationException($"Model {name} contains no meshes");
        
        Span<(int vertexCount, int indexCount)> dataCount = stackalloc (int, int)[meshCount];
        IterateMeshes(scene, dataCount);

        if (HasAnimationChannels(scene) && Ctx.BoneIndexByName.Count > 0)
        {
            var animationCount = (int)scene->MNumAnimations;
            Ctx.Animation = new ModelAnimation(
                animationCount,
                new Dictionary<string, int>(Ctx.BoneIndexByName),
                in scene->MRootNode->MTransformation);
        }

        Ctx.Being(name, path, dataCount);
    }

    public void End(AssimpScene* scene, AssetGfxUploader gfxUploader)
    {
        var model = Ctx.Model;
        var animation = Ctx.Animation;

        foreach (var mesh in model.Meshes)
        {
            var info = new MeshCreationInfo();
            if (animation != null)
            {
                var meshSpan = Ctx.Scratchpad.GetSkinnedMeshSpan(mesh.Info.MeshIndex);
                var payload = new MeshUploadData<VertexSkinned>(meshSpan.Vertices, meshSpan.Indices, ref info);
                gfxUploader.UploadMesh(payload);
            }
            else
            {
                var meshSpan = Ctx.Scratchpad.GetMeshSpan(mesh.Info.MeshIndex);
                var payload = new MeshUploadData<Vertex3D>(meshSpan.Vertices, meshSpan.Indices, ref info);
                gfxUploader.UploadMesh(payload);
            }

            mesh.MeshId = info.MeshId;
        }

        var bounds = model.Meshes[0].LocalBounds;
        for (var i = 1; i < model.Meshes.Length; i++)
            BoundingBox.Merge(in bounds, in model.Meshes[i].LocalBounds, out bounds);

        model.ModelBounds = bounds;

        _assimp.FreeScene(scene);
    }

    private void PreProcessScene(AssimpScene* scene)
    {
        if (_assimp is null) throw new InvalidOperationException(nameof(_assimp));
        _assimp.Matrix4Inverse(&scene->MRootNode->MTransformation);
        _assimp.TransposeMatrix4(&scene->MRootNode->MTransformation);

        TraverseTranspose(_assimp, scene->MRootNode);
        return;

        static void TraverseTranspose(Assimp assimp, AssimpNode* currentNode)
        {
            for (var i = 0; i < currentNode->MNumChildren; i++)
            {
                var node = currentNode->MChildren[i];
                Ctx.NodeMap[node->MName] = (IntPtr)node;
                assimp.TransposeMatrix4(&node->MTransformation);
                TraverseTranspose(assimp, node);
            }
        }
    }

    private void IterateMeshes(AssimpScene* scene, Span<(int, int)> dataCount)
    {
        if (_assimp is null) throw new InvalidOperationException(nameof(_assimp));

        var numMeshes = (int)scene->MNumMeshes;
        var model = Ctx.Model = new ModelData(numMeshes);

        for (var i = 0; i < numMeshes; i++)
        {
            var aiMesh = scene->MMeshes[i];
            var boneCount = aiMesh->MNumBones;
            for (var b = 0; b < boneCount; b++)
            {
                var bone = aiMesh->MBones[b];
                _assimp.TransposeMatrix4(&bone->MOffsetMatrix);
                RegisterBoneRecursive(bone->MName.AsString);
            }
        }

        for (var i = 0; i < numMeshes; i++)
        {
            var meshIndex = (byte)i;

            var aiMesh = scene->MMeshes[meshIndex];
            int vertCount = (int)aiMesh->MNumVertices, faceCount = (int)aiMesh->MNumFaces;
            dataCount[meshIndex] = (vertCount, faceCount * 3);
            model.TotalVertexCount += vertCount;
            model.TotalFaceCount += faceCount;

            byte boneCount = (byte)aiMesh->MNumBones, materialIndex = (byte)aiMesh->MMaterialIndex;
            Ctx.Model.Meshes[meshIndex] = new MeshEntry
            {
                Name = aiMesh->MName.AsString,
                Info = new MeshInfo(vertCount, faceCount, meshIndex, materialIndex, boneCount)
            };

            for (var b = 0; b < boneCount; b++)
            {
                var bone = aiMesh->MBones[i];
                var boneIndex = Ctx.BoneIndexByName[bone->MName];
                Ctx.BoneIndexByMeshBone[(meshIndex, b)] = boneIndex;
                //Ctx.OffsetMatrices[boneIndex] = bone->MOffsetMatrix;
            }
        }

        return;

        void RegisterBoneRecursive(string name)
        {
            if (Ctx.BoneIndexByName.ContainsKey(name)) return;
            if (Ctx.NodeMap.TryGetValue(name, out var nodePtr))
            {
                var node = (AssimpNode*)nodePtr;
                if (node->MParent != null)
                    RegisterBoneRecursive(node->MParent->MName.AsString);
            }

            Ctx.BoneIndexByName[name] = Ctx.BoneIndexByName.Count;
        }
    }

    //
    private static bool HasAnimationChannels(AssimpScene* scene)
    {
        for (uint i = 0; i < scene->MNumAnimations; i++)
        {
            var anim = scene->MAnimations[i];
            if (anim->MNumChannels > 0) return true;
        }

        return false;
    }
    //


    private static void Traverse(AssimpNode* node, in Matrix4x4 parentWorld)
    {
        if (node == null) return;

        var local = node->MTransformation;
        MatrixMath.MultiplyAffine(in local, in parentWorld, out var world);

        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var meshIndex = (int)node->MMeshes[i];
            Ctx.Model.WorldTransforms[meshIndex] = world;
        }

        for (var i = 0; i < node->MNumChildren; i++)
        {
            if (node->MChildren[i] == null) continue;
            Traverse(node->MChildren[i], in world);
        }

        if (Ctx.Animation is { } animation && Ctx.BoneIndexByName.TryGetValue(node->MName.AsString, out var boneIndex))
        {
            ref readonly var skeleton = ref animation.SkeletonData;
            skeleton.BindPose[boneIndex] = local;
            //Matrix4x4.Invert(local, out skeleton.InverseBindPose[boneIndex]); // OffsetMatrix

            var parent = node->MParent;
            if (node->MParent != null && Ctx.BoneIndexByName.TryGetValue(parent->MName.AsString, out var parentIdx))
            {
                skeleton.ParentIndices[boneIndex] = parentIdx;
            }
            else
            {
                //skeleton.InverseBindPose[boneIndex] = local;
                skeleton.ParentIndices[boneIndex] = -1;
            }
        }
    }

    private static void ProcessMeshData(AssimpMesh* aiMesh, int meshIndex, ModelData model)
    {
        if (Ctx.Animation == null)
        {
            var meshSpan = Ctx.Scratchpad.GetMeshSpan(meshIndex);
            WriteIndices(aiMesh, meshSpan.Indices);
            WriteVertices(aiMesh, meshIndex, model, meshSpan.Vertices);
        }
        else
        {
            var meshSpan = Ctx.Scratchpad.GetSkinnedMeshSpan(meshIndex);
            WriteIndices(aiMesh, meshSpan.Indices);
            WriteSkinningData(aiMesh, meshIndex, Ctx.BoneIndexByMeshBone, meshSpan.Skinned);
            WriteVerticesSkinned(aiMesh, meshIndex, model, meshSpan.Vertices, meshSpan.Skinned);
        }
    }


    private static void ProcessAnimation(AssimpAnimation* aiAnim)
    {
        var animation = Ctx.Animation;
        if (animation == null) throw new InvalidOperationException(nameof(animation));

        var name = aiAnim->MName.AsString;
        var duration = (float)aiAnim->MDuration;
        var ticksPerSecond = (float)(aiAnim->MTicksPerSecond != 0 ? aiAnim->MTicksPerSecond : 25.0f);

        var channels = (int)aiAnim->MNumChannels;

        var clip = new AnimationClip(name, animation.BoneCount, duration, ticksPerSecond);
        animation.Clips.Add(clip);

        for (uint c = 0; c < channels; c++)
        {
            var aiChannel = aiAnim->MChannels[c];
            var boneName = aiChannel->MNodeName.AsString;
            if (!animation.BoneMapping.TryGetValue(boneName, out var boneIndex))
                continue;

            // Position
            var posKeys = aiChannel->MPositionKeys;
            var posCount = (int)aiChannel->MNumPositionKeys;

            var rotKeys = aiChannel->MRotationKeys;
            var rotCount = (int)aiChannel->MNumRotationKeys;

            var channel = new AnimationChannel(posCount, rotCount);

            for (var k = 0; k < posCount; k++)
            {
                channel.PositionTimes[k] = (float)posKeys[k].MTime;
                channel.Positions[k] = posKeys[k].MValue;
            }

            for (var k = 0; k < rotCount; k++)
            {
                channel.RotationTimes[k] = (float)rotKeys[k].MTime;
                channel.Rotations[k] = rotKeys[k].MValue.AsQuaternion;
            }

            clip.Channels[boneIndex] = new AnimationClip.ChannelEntry(channel);
        }
    }
}