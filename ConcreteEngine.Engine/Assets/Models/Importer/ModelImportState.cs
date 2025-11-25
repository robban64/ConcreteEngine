using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Models.Loader;
using Silk.NET.Assimp;
using static ConcreteEngine.Engine.Assets.Models.Importer.Constants;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMaterial = Silk.NET.Assimp.Material;

namespace ConcreteEngine.Engine.Assets.Models.Importer;

internal sealed class ModelImportState
{
    // Mesh
    private readonly List<string> _meshNames = new(MaxParts);
    private readonly Dictionary<int, MeshCreationInfo> _meshIndexToIdMap = new(8);

    //Animation
    private bool _hasValidAnimation = false;
    private readonly List<int> _parentIndices = new(8);
    private readonly List<ModelAnimationData> _animations = new(8);
    private readonly Dictionary<string, int> _boneByName = new(8);

    // Material/Textures
    private List<TextureEmbeddedDescriptor> _embeddedTextures = new(4);
    private List<ModelMaterialEmbeddedDescriptor> _embeddedMaterials = new(4);

    public int BoneCount => _boneByName.Count;
    public int MeshCount => _meshNames.Count;

    public void PrepareAnimationState(int animationLen, Span<int> defaultIndices)
    {
        _animations.Clear();
        _animations.EnsureCapacity(animationLen);

        _parentIndices.Clear();
        _parentIndices.AddRange(defaultIndices);
    }

    public bool HasProcessedMeshIndex(int meshIndex, out MeshCreationInfo info) =>
        _meshIndexToIdMap.TryGetValue(meshIndex, out info);

    public void AppendMeshInfo(string name, int meshIndex, MeshCreationInfo creationInfo)
    {
        _meshIndexToIdMap.Add(meshIndex, creationInfo);
        _meshNames.Add(name);
    }


    public bool TryGetBoneIndex(string boneName, out int index) => _boneByName.TryGetValue(boneName, out index);
    public void AppendBone(string boneName, int index) => _boneByName.Add(boneName, index);
    public void AppendAnimation(ModelAnimationData animation) => _animations.Add(animation);

    public void UpdateBoneParentIndexOrDefault(string parentName, int boneIndex)
    {
        if (TryGetBoneIndex(parentName, out int parentBoneIndex))
            _parentIndices[boneIndex] = parentBoneIndex;
        else
            _parentIndices[boneIndex] = -1;
    }


    public void AppendMaterial(ModelMaterialEmbeddedDescriptor descriptor) => _embeddedMaterials.Add(descriptor);

    public void AppendTexture(TextureEmbeddedDescriptor descriptor)
    {
        descriptor.Index = _embeddedTextures.Count;
        _embeddedTextures.Add(descriptor);
    }

    public TextureEmbeddedDescriptor? FindTextureByName(string name)
    {
        foreach (var it in _embeddedTextures)
        {
            if (it.EmbeddedName == name) return it;
        }

        return null;
    }
}