using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;

namespace ConcreteEngine.Engine.Assets.Models.Loader.AssimpImporter;

internal sealed class AssimpSceneProcessor(ModelLoaderDataTable dataTable, ModelLoaderState state)
{
    private readonly Dictionary<string, IntPtr> _nodeMap = new(16);

    
    private unsafe void PreProcessScene(Assimp assimp, AssimpScene* scene)
    {
        TraverseTranspose(assimp, scene->MRootNode);
        TransposeBones();
        return;

        void TransposeBones()
        {
            for (int j = 0; j < scene->MNumMeshes; j++)
            {
                var mesh = scene->MMeshes[j];
                for (int b = 0; b < mesh->MNumBones; b++)
                {
                    var bone = mesh->MBones[b];
                    assimp!.TransposeMatrix4(&bone->MOffsetMatrix);
                }
            }
        }

        static void TraverseTranspose(Assimp assimp, AssimpNode* currentNode)
        {
            for (int i = 0; i < currentNode->MNumChildren; i++)
            {
                var node = currentNode->MChildren[i];
                assimp!.TransposeMatrix4(&node->MTransformation);
                TraverseTranspose(assimp, node);
            }
        }
    }
    
    private unsafe void MapNodes(AssimpNode* node)
    {
        _nodeMap[node->MName.AsString] = (IntPtr)node;
        for (int i = 0; i < node->MNumChildren; i++)
            MapNodes(node->MChildren[i]);
    }


    private unsafe void RegisterAllBones(AssimpScene* scene)
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

            int idx = state.BoneCount;
            state.AppendBone(name, idx);
        }
    }


}