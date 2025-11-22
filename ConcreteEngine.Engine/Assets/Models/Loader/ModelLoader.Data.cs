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


internal ref struct ModelImportResult(
    ReadOnlySpan<string> partNames,
    ReadOnlySpan<MeshPartImportResult> parts,
    ReadOnlySpan<Matrix4x4> partTransforms,
    ref readonly BoundingBox bounds)
{
    public readonly ReadOnlySpan<string> PartNames = partNames;
    public readonly ReadOnlySpan<MeshPartImportResult> Parts = parts;
    public readonly ReadOnlySpan<Matrix4x4> PartTransforms = partTransforms;
    public ref readonly BoundingBox Bounds = ref bounds;

    public int Count => PartNames.Length;
}


internal ref struct AnimationImportResult(
    ReadOnlySpan<Matrix4x4> boneTransforms,
    ref Matrix4x4 invRootTransform,
    ReadOnlyDictionary<string, int>? boneMapping
)
{
    public ReadOnlySpan<Matrix4x4> BoneTransforms  = boneTransforms;
    public ref Matrix4x4 InvRootTransform  = ref invRootTransform;
    public readonly ReadOnlyDictionary<string, int>? BoneMapping  = boneMapping; // index to bone transform
}
