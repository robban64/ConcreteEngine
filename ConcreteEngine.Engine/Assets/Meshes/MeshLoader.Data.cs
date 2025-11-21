#region

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Engine.Assets.Meshes;

internal readonly record struct MeshCreationInfo(MeshId MeshId, int DrawCount);

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

[StructLayout(LayoutKind.Sequential)]
internal struct SkinningData
{
    public Int4 BoneIndices;
    public Vector4 BoneWeights;

    public int GetVertexId(int idx)
    {
        Debug.Assert(idx < 4);
        ref var i0 = ref Unsafe.AsRef(ref BoneIndices.X);
        return Unsafe.Add(ref i0, idx);
    }

    public void Set(int idx, int vertexIndex, float boneWeight)
    {
        Debug.Assert(idx < 4);

        ref var i0 = ref Unsafe.AsRef(ref BoneIndices.X);
        Unsafe.Add(ref i0, idx) = vertexIndex;

        ref var w0 = ref Unsafe.AsRef(ref BoneWeights.X);
        Unsafe.Add(ref w0, idx) = boneWeight;
    }
}