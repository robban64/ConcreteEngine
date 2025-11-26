#region

using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Descriptors;
using static ConcreteEngine.Engine.Assets.Models.ImportProcessors.ImportConstants;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Loader;

internal ref struct ModelLoaderResult(int drawCount, in BoundingBox bounds)
{
    public readonly int DrawCount = drawCount;
    public ref readonly BoundingBox Bounds = ref bounds;

    public required ModelAnimation? Animation { get; init; }

    public required ModelMesh[] MeshParts { get; init; }

    // Descriptors
    public required ReadOnlySpan<MaterialEmbeddedDescriptor> EmbeddedMaterials { get; init; }
    public required ReadOnlySpan<TextureEmbeddedDescriptor> EmbeddedTextures { get; init; }
}

internal sealed class ModelLoaderState
{
    // Mesh
    private readonly List<string> _meshNames = new(MaxParts);
    private readonly Dictionary<int, MeshCreationInfo> _meshIndexToIdMap = new(8);

    //Animation
    private readonly List<int> _parentIndices = new(8);
    private readonly List<ModelAnimationData> _animations = new(8);
    private readonly Dictionary<string, int> _boneByName = new(8);

    // Material/Textures
    private readonly List<TextureEmbeddedDescriptor> _embeddedTextures = new(4);
    private readonly List<MaterialEmbeddedDescriptor> _embeddedMaterials = new(4);

    public string Name { get; private set; }
    public string Filename { get; private set; }

    public bool MightBeAnimated { get; set; }
    public bool HasAnimationChannels { get; set; }

    public string ToEmbeddedAssetName(string type, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        InvalidOpThrower.ThrowIfNull(Name, nameof(Name));
        InvalidOpThrower.ThrowIfNull(Filename, nameof(Filename));

        return $"{Name}::{type}/{index}";
    }

    public int BoneCount => _boneByName.Count;
    public int MeshCount => _meshNames.Count;
    public bool HasEmbeddedData => _embeddedMaterials.Count > 0;

    public bool IsAnimated =>
        HasAnimationChannels || _boneByName.Count > 0 && _animations.Count > 0 && _parentIndices.Count > 0;


    public void Start(string name, string filename)
    {
        Clear();
        Name = name;
        Filename = filename;
    }

    public ModelLoaderResult BuildResult(ModelMesh[] meshParts, ModelAnimation? animation, int drawCount,
        ref readonly BoundingBox bounds)
    {
        ArgumentNullException.ThrowIfNull(meshParts);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(drawCount, 0);

        return new ModelLoaderResult(drawCount, in bounds)
        {
            Animation = animation,
            MeshParts = meshParts,
            EmbeddedMaterials = CollectionsMarshal.AsSpan(_embeddedMaterials),
            EmbeddedTextures = CollectionsMarshal.AsSpan(_embeddedTextures)
        };
    }


    public string GetMeshName(int meshIndex) => _meshNames[meshIndex];

    public bool HasProcessedMeshIndex(int meshIndex, out MeshCreationInfo info) =>
        _meshIndexToIdMap.TryGetValue(meshIndex, out info);

    public void AppendMeshInfo(string name, int meshIndex, MeshCreationInfo creationInfo)
    {
        _meshIndexToIdMap.Add(meshIndex, creationInfo);
        _meshNames.Add(name);
    }


    public void GetAnimationResult(out IReadOnlyDictionary<string, int> boneMapping,
        out ReadOnlySpan<ModelAnimationData> animations, out ReadOnlySpan<int> parentIndices)
    {
        ArgumentOutOfRangeException.ThrowIfZero(_boneByName.Count);
        ArgumentOutOfRangeException.ThrowIfZero(_parentIndices.Count);
        ArgumentOutOfRangeException.ThrowIfZero(_animations.Count);

        boneMapping = _boneByName;
        parentIndices = CollectionsMarshal.AsSpan(_parentIndices);
        animations = CollectionsMarshal.AsSpan(_animations);
    }

    public void PrepareAnimationState(int animationLen, Span<int> defaultIndices)
    {
        ArgumentOutOfRangeException.ThrowIfZero(animationLen);
        InvalidOpThrower.ThrowIf(_animations.Count > 0 || _parentIndices.Count > 0);
        _animations.EnsureCapacity(animationLen);
        _parentIndices.AddRange(defaultIndices);
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


    public void AppendMaterial(MaterialEmbeddedDescriptor descriptor) => _embeddedMaterials.Add(descriptor);

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

    public void Clear()
    {
        _meshNames.Clear();
        _meshIndexToIdMap.Clear();
        _parentIndices.Clear();
        _animations.Clear();
        _boneByName.Clear();
        _embeddedTextures.Clear();
        _embeddedMaterials.Clear();

        HasAnimationChannels = false;
        MightBeAnimated = false;

        Name = string.Empty;
        Filename = string.Empty;
    }
}