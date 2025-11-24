using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Engine.Assets.Models;

public sealed class ModelAnimation
{
    
    private readonly ModelAnimationData[] _animations;
    private readonly int[] _parentIndices;
    private readonly Dictionary<int, string> _boneMapping;
    private readonly Matrix4x4[] _boneTransforms;
    private readonly Matrix4x4 _inverseRootTransform;
    

    internal ModelAnimation(
        IReadOnlyDictionary<string, int> boneMapping,
        ReadOnlySpan<ModelAnimationData> animations,
        ReadOnlySpan<int> parentIndices,
        ReadOnlySpan<Matrix4x4> boneTransforms,
        in Matrix4x4 inverseRootTransform)
    {
        _animations = animations.ToArray();
        _parentIndices = parentIndices.ToArray();
        _boneTransforms = boneTransforms.ToArray();
        _inverseRootTransform = inverseRootTransform;

        _boneMapping = new Dictionary<int, string>(boneMapping.Count);
        foreach (var (key, value) in boneMapping)
        {
            _boneMapping.Add(value, key);
        }
    }

    public ReadOnlySpan<int> ParentIndices => _parentIndices;

    public ref readonly Matrix4x4 InverseRootTransform => ref _inverseRootTransform;
    public ReadOnlySpan<Matrix4x4> GetBoneTransformSpan() => _boneTransforms;
    internal ReadOnlySpan<ModelAnimationData> AnimationDataSpan => _animations;
    internal Dictionary<int, string> BoneIndexByName() => _boneMapping;

    public int BoneCount => _boneTransforms.Length;
}
