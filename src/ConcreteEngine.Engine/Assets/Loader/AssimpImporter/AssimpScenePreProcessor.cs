using ConcreteEngine.Engine.Assets.Loader.State;
using Silk.NET.Assimp;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;

namespace ConcreteEngine.Engine.Assets.Loader.AssimpImporter;

internal sealed class AssimpScenePreProcessor(ModelLoaderState state)
{
    private readonly Dictionary<string, IntPtr> _nodeMap = new(16);

    internal void Clear() => _nodeMap.Clear();

    internal unsafe void PreProcessSceneGraph(Assimp assimp, AssimpScene* scene, ModelLoaderDataTable dataTable)
    {
        if (_nodeMap.Count > 0) throw new InvalidOperationException(nameof(_nodeMap));

        state.MightBeAnimated = scene->MNumAnimations > 0 || scene->MNumSkeletons > 0;

        assimp!.Matrix4Inverse(&scene->MRootNode->MTransformation);
        assimp!.TransposeMatrix4(&scene->MRootNode->MTransformation);
        dataTable.InvRootTransform = scene->MRootNode->MTransformation;
        //Matrix4x4.Invert(scene->MRootNode->MTransformation, out var invRoot);
        //dataTable.InvRootTransform = Matrix4x4.Transpose(invRoot);

        ProcessNodes(assimp, scene->MRootNode);
        IterateMeshes(assimp, scene);
        state.HasAnimationChannels = HasAnimationChannels(scene);
    }

    private unsafe void ProcessNodes(Assimp assimp, AssimpNode* currentNode)
    {
        for (int i = 0; i < currentNode->MNumChildren; i++)
        {
            var node = currentNode->MChildren[i];
            _nodeMap[node->MName.AsString] = (IntPtr)node;
            assimp!.TransposeMatrix4(&node->MTransformation);
            ProcessNodes(assimp, node);
        }
    }

    private unsafe void IterateMeshes(Assimp assimp, AssimpScene* scene)
    {
        for (int j = 0; j < scene->MNumMeshes; j++)
        {
            var mesh = scene->MMeshes[j];
            for (int b = 0; b < mesh->MNumBones; b++)
            {
                var bone = mesh->MBones[b];
                var boneName = mesh->MBones[b]->MName.AsString;
                assimp!.TransposeMatrix4(&bone->MOffsetMatrix);
                RegisterBoneRecursive(boneName);
            }
        }

        return;

        void RegisterBoneRecursive(string name)
        {
            if (state.TryGetBoneIndex(name, out _)) return;
            if (_nodeMap.TryGetValue(name, out IntPtr nodePtr))
            {
                var node = (AssimpNode*)nodePtr;
                if (node->MParent != null)
                    RegisterBoneRecursive(node->MParent->MName.AsString);
            }

            var idx = state.BoneCount;
            state.AppendBone(name, idx);
        }
    }

    private unsafe bool HasAnimationChannels(AssimpScene* scene)
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
    /*
    void RegisterBones(AssimpScene* scene)
    {
        for (uint m = 0; m < scene->MNumMeshes; m++)
        {
            var mesh = scene->MMeshes[m];
            for (uint b = 0; b < mesh->MNumBones; b++)
            {
                var boneName = mesh->MBones[b]->MName.AsString;
                RegisterBoneRecursive(boneName);
            }
        }

    }*/
}