using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Assets.Descriptors;
using static ConcreteEngine.Engine.Assets.Models.Loader.AssimpImporter.ImportModelUtils;

namespace ConcreteEngine.Engine.Assets.Models.Loader;

internal ref struct ModelLoaderResult(long fileSize, int drawCount, in BoundingBox bounds)
{
    // Descriptors
    public required ReadOnlySpan<EmbeddedRecord> Embedded;

    public long FileSize = fileSize;
    public ref readonly BoundingBox Bounds = ref bounds;
    public required ModelAnimation? Animation;
    public required ModelMesh[] MeshParts;

    public readonly int DrawCount = drawCount;
}

internal sealed class ModelLoaderState
{
    // Mesh
    private readonly List<string> _meshNames = new(MaxParts);
    private readonly Dictionary<int, MeshCreationInfo> _meshIndexToIdMap = new(8);

    //Animation
    private readonly List<int> _parentIndices = new(8);
    private readonly List<AnimationClip> _animations = new(8);
    private readonly Dictionary<string, int> _boneByName = new(8);

    // Material/Textures
    public readonly List<EmbeddedRecord> EmbeddedList = new(4);

    public string Name { get; private set; }
    public string Filename { get; private set; }

    public bool MightBeAnimated { get; set; }
    public bool HasAnimationChannels { get; set; }


    public int BoneCount => _boneByName.Count;
    public int MeshCount => _meshNames.Count;

    public bool IsAnimated =>
        HasAnimationChannels || _boneByName.Count > 0 && _animations.Count > 0 && _parentIndices.Count > 0;

    public string ToEmbeddedAssetName(string type, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        InvalidOpThrower.ThrowIfNull(Name, nameof(Name));
        InvalidOpThrower.ThrowIfNull(Filename, nameof(Filename));

        return $"{Name}::{type}/{index}";
    }

    public void Start(string name, string filename)
    {
        Clear();
        Name = name;
        Filename = filename;
    }

    public ModelLoaderResult BuildResult(long fileSize, ModelMesh[] meshParts, ModelAnimation? animation, int drawCount,
        ref readonly BoundingBox bounds)
    {
        ArgumentNullException.ThrowIfNull(meshParts);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(drawCount, 0);

        return new ModelLoaderResult(fileSize, drawCount, in bounds)
        {
            Animation = animation,
            MeshParts = meshParts,
            Embedded = CollectionsMarshal.AsSpan(EmbeddedList)
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
        out ReadOnlySpan<AnimationClip> animations, out ReadOnlySpan<int> parentIndices)
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
    public void AppendAnimation(AnimationClip animation) => _animations.Add(animation);

    public void UpdateBoneParentIndexOrDefault(string parentName, int boneIndex)
    {
        if (TryGetBoneIndex(parentName, out int parentBoneIndex))
            _parentIndices[boneIndex] = parentBoneIndex;
        else
            _parentIndices[boneIndex] = -1;
    }

    public void Clear()
    {
        _meshNames.Clear();
        _meshIndexToIdMap.Clear();
        _parentIndices.Clear();
        _animations.Clear();
        _boneByName.Clear();
        EmbeddedList.Clear();

        HasAnimationChannels = false;
        MightBeAnimated = false;

        Name = string.Empty;
        Filename = string.Empty;
    }
}