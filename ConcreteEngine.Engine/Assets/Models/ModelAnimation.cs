#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Engine.Assets.Models;

public sealed class ModelAnimation
{
    private readonly AnimationClip[] _clips;

    private readonly Dictionary<int, string> _boneMapping;

    private readonly int[] _parentIndices;
    private readonly Matrix4x4[] _boneOffsetMatrix;
    private readonly Matrix4x4[] _nodeTransforms;
    private readonly Matrix4x4 _inverseRootTransform;
    private readonly Matrix4x4 _skeletonRootOffset;

    public string ModelName { get; internal set; } = string.Empty;

    internal ModelAnimation(
        IReadOnlyDictionary<string, int> boneMapping,
        ReadOnlySpan<AnimationClip> clips,
        ReadOnlySpan<int> parentIndices,
        ReadOnlySpan<Matrix4x4> boneOffsetMatrix,
        ReadOnlySpan<Matrix4x4> nodeTransforms,
        in Matrix4x4 inverseRootTransform,
        in Matrix4x4 skeletonRootOffset)
    {
        _clips = clips.ToArray();
        _parentIndices = parentIndices.ToArray();
        _boneOffsetMatrix = boneOffsetMatrix.ToArray();
        _nodeTransforms = nodeTransforms.ToArray();
        _inverseRootTransform = inverseRootTransform;
        _skeletonRootOffset = skeletonRootOffset;

        _boneMapping = new Dictionary<int, string>(boneMapping.Count);
        foreach (var (key, value) in boneMapping)
        {
            _boneMapping.Add(value, key);
        }
    }


    public ref readonly Matrix4x4 InverseRootTransform => ref _inverseRootTransform;
    public ref readonly Matrix4x4 SkeletonRootOffset => ref _skeletonRootOffset;

    public ReadOnlySpan<int> ParentIndexSpan => _parentIndices;
    public ReadOnlySpan<Matrix4x4> BoneOffsetMatrixSpan => _boneOffsetMatrix;
    public ReadOnlySpan<Matrix4x4> NodeTransformSpan => _nodeTransforms;
    internal ReadOnlySpan<AnimationClip> ClipDataSpan => _clips;
    public int DefinedBoneCount => _boneOffsetMatrix.Length;
    
    public AnimationClip this[int index] => _clips[index];
}