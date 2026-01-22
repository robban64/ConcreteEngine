using System.Numerics;
using ConcreteEngine.Core.Renderer;

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

    public AnimationId AnimationId { get; private set; }

    public ModelAnimation(
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

    public int ClipCount => _clips.Length;
    public int BoneCount => _boneMapping.Count;

    public void Attach(AnimationId animationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(animationId.Value, nameof(animationId));
        if (AnimationId > 0) throw new InvalidOperationException("Animation already attached.");
        AnimationId = animationId;
    }


    public ref readonly Matrix4x4 InverseRootTransform => ref _inverseRootTransform;
    public ref readonly Matrix4x4 SkeletonRootOffset => ref _skeletonRootOffset;

    public ReadOnlySpan<int> ParentIndexSpan => _parentIndices;
    public ReadOnlySpan<Matrix4x4> BoneOffsetMatrixSpan => _boneOffsetMatrix;
    public ReadOnlySpan<Matrix4x4> NodeTransformSpan => _nodeTransforms;
    public ReadOnlySpan<AnimationClip> ClipDataSpan => _clips;
    public int DefinedBoneCount => _boneOffsetMatrix.Length;
}