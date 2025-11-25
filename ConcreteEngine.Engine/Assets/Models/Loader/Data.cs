#region

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Loader;

internal readonly struct MeshCreationInfo(MeshId meshId, int drawCount)
{
    public readonly MeshId MeshId = meshId;
    public readonly int DrawCount = drawCount;
}

internal struct MeshPartImportResult(
    int materialSlot,
    MeshCreationInfo creationInfo,
    in BoundingBox bounds)
{
    public int MaterialSlot = materialSlot;
    public MeshCreationInfo CreationInfo = creationInfo;
    public BoundingBox Bounds = bounds;
}

internal ref struct MeshUploadData<TVertex>(
    ReadOnlySpan<TVertex> vertices,
    ReadOnlySpan<uint> indices,
    ref MeshCreationInfo result) where TVertex : unmanaged
{
    public ReadOnlySpan<TVertex> Vertices = vertices;
    public ReadOnlySpan<uint> Indices = indices;
    public ref MeshCreationInfo Result = ref result;
}

internal readonly ref struct ModelImportResult(
    ReadOnlySpan<MeshPartImportResult> parts,
    ReadOnlySpan<Matrix4x4> partTransforms,
    in BoundingBox bounds)
{
    public readonly ReadOnlySpan<MeshPartImportResult> Parts = parts;
    public readonly ReadOnlySpan<Matrix4x4> PartTransforms = partTransforms;
    public readonly ref readonly BoundingBox Bounds = ref bounds;
}

internal readonly ref struct AnimationImportResult(
    IReadOnlyDictionary<string, int> boneMapping,
    ReadOnlySpan<ModelAnimationData> animations,
    ReadOnlySpan<int> parentIndices,
    ReadOnlySpan<Matrix4x4> boneTransforms,
    ref Matrix4x4 invRootTransform
)
{
    public readonly IReadOnlyDictionary<string, int> BoneMapping = boneMapping;
    public readonly ReadOnlySpan<ModelAnimationData> Animations = animations;
    public readonly ReadOnlySpan<int> ParentIndices = parentIndices;
    public readonly ReadOnlySpan<Matrix4x4> BoneTransforms = boneTransforms;
    public readonly ref Matrix4x4 InvRootTransform = ref invRootTransform;
}