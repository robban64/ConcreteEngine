using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Engine.Assets.Models;

public sealed class ModelAnimation(
    Dictionary<string, int> boneMapping,
    List<ModelAnimationData> animations,
    Matrix4x4[] boneTransforms,
    in Matrix4x4 inverseRootTransform,
    List<int> parentIndices)
{
    
    private readonly List<ModelAnimationData> _animations = animations;
    private readonly Dictionary<string, int> _boneMapping = boneMapping;
    private readonly Matrix4x4[] _boneTransforms = boneTransforms;
    private readonly Matrix4x4 _inverseRootTransform = inverseRootTransform;
    
    private readonly List<int> _parentIndices = parentIndices; 
    
    public ReadOnlySpan<int> ParentIndices => CollectionsMarshal.AsSpan(_parentIndices);

    public ref readonly Matrix4x4 InverseRootTransform => ref _inverseRootTransform;
    public ReadOnlySpan<Matrix4x4> GetBoneTransformSpan() => _boneTransforms;
    internal ReadOnlySpan<ModelAnimationData> AnimationDataSpan => CollectionsMarshal.AsSpan(_animations);
    internal Dictionary<string, int> GetBoneMapping() => _boneMapping;

    public int BoneCount => _boneTransforms.Length;
}
