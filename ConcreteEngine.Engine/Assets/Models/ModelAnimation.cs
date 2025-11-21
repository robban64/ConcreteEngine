using System.Numerics;

namespace ConcreteEngine.Engine.Assets.Models;

public sealed class ModelAnimation(
    Dictionary<string, int> boneMapping,
    Matrix4x4[] boneTransforms,
    in Matrix4x4 inverseRootTransform)
{
    private readonly Dictionary<string, int> _boneMapping = boneMapping;
    private readonly Matrix4x4[] _boneTransforms = boneTransforms;
    private readonly Matrix4x4 _inverseRootTransform = inverseRootTransform;

    public ref readonly Matrix4x4 InverseRootTransform => ref _inverseRootTransform;
    public ReadOnlySpan<Matrix4x4> GetBoneTransformSpan() => _boneTransforms;

    public int BoneCount => _boneTransforms.Length;
}
