using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace ConcreteEngine.Engine.Assets.ImporterAssimp;

internal sealed unsafe partial class ModelImporter
{
    private static void WriteIndicesU32(AssimpMesh* mesh, NativeView<uint> indices)
    {
        var faceLen = mesh->MNumFaces;
        var faces = mesh->MFaces;
        var ptr = indices.Ptr;
        for (var i = 0; i < faceLen; i++)
        {
            var face = faces[i];
            *ptr++ = face.MIndices[0];
            *ptr++ = face.MIndices[1];
            *ptr++ = face.MIndices[2];
        }
    }

    private static void WriteIndicesU16(AssimpMesh* mesh, NativeView<ushort> indices)
    {
        var faceLen = mesh->MNumFaces;
        var faces = mesh->MFaces;
        var ptr = indices.Ptr;
        for (var i = 0; i < faceLen; i++)
        {
            var face = faces[i];
            *ptr++ = (ushort)face.MIndices[0];
            *ptr++ = (ushort)face.MIndices[1];
            *ptr++ = (ushort)face.MIndices[2];
        }
    }

    private static void WriteVertices(
        AssimpMesh* aiMesh,
        int meshIndex,
        ModelImportData model,
        NativeView<Vertex3D> vertices)
    {
        var count = (int)aiMesh->MNumVertices;
        ArgumentOutOfRangeException.ThrowIfLessThan(vertices.Length, count, nameof(vertices.Length));

        var meshEntry = model.Meshes[meshIndex];
        var bounds = BoundingBox.Infinite;
        var texCoords = aiMesh->MTextureCoords[0];
        for (int i = 0; i < count; i++)
        {
            ref var v = ref vertices[i];
            v.Position = aiMesh->MVertices[i];
            v.TexCoords = texCoords[i].AsVector2();
            v.Normal = aiMesh->MNormals[i];
            v.Tangent = aiMesh->MTangents[i];
            bounds.FromPoint(v.Position);
        }

        meshEntry.LocalBounds = bounds;
    }

    private static void WriteSkinningData(AssimpMesh* aMesh, ModelRig rig,
        NativeView<SkinningData> vertices)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)aMesh->MNumBones, AssimpUtils.BoneLimit);

        // clear
        vertices.AsSpan().Fill(SkinningData.Identity);

        // write
        {
            var boneLen = aMesh->MNumBones;
            var bones = aMesh->MBones;
            var inverseBindPose = rig.InverseBindPose;
            for (var i = 0; i < boneLen; i++)
            {
                var bone = bones[i];
                TryGetBoneIndex(AssimpUtils.GetNameHash(bone->MName), out var boneIndex);
                inverseBindPose[boneIndex] = bone->MOffsetMatrix;

                WriteWeightAndIndices(bone, boneIndex, vertices);
            }
        }

        // sanitize
        for (var i = 0; i < vertices.Length; i++)
        {
            ref var boneIndices = ref vertices[i].BoneIndices;
            boneIndices.X = int.Max(boneIndices.X, 0);
            boneIndices.Y = int.Max(boneIndices.Y, 0);
            boneIndices.Z = int.Max(boneIndices.Z, 0);
            boneIndices.W = int.Max(boneIndices.W, 0);
        }
    }

    private static void WriteWeightAndIndices(Bone* bone, int boneIndex, NativeView<SkinningData> skinningData)
    {
        var weightLen = bone->MNumWeights;
        var weights = bone->MWeights;

        for (var j = 0; j < weightLen; j++)
        {
            var weight = weights[j];

            if (weight.MVertexId >= skinningData.Length) continue;

            ref var data = ref *(skinningData.Ptr + weight.MVertexId);

            if (data.BoneIndices.X < 0)
            {
                data.BoneIndices.X = boneIndex;
                data.BoneWeights.X = weight.MWeight;
            }
            else if (data.BoneIndices.Y < 0)
            {
                data.BoneIndices.Y = boneIndex;
                data.BoneWeights.Y = weight.MWeight;
            }
            else if (data.BoneIndices.Z < 0)
            {
                data.BoneIndices.Z = boneIndex;
                data.BoneWeights.Z = weight.MWeight;
            }
            else if (data.BoneIndices.W < 0)
            {
                data.BoneIndices.W = boneIndex;
                data.BoneWeights.W = weight.MWeight;
            }
        }
    }
}