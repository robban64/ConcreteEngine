using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace ConcreteEngine.Engine.Assets.ImporterAssimp;

internal sealed unsafe partial class ModelImporter
{
    private static void WriteIndicesU32(AssimpMesh* mesh, NativeView<uint> indices)
    {
        var length = mesh->MNumFaces;
        var faces = mesh->MFaces;
        var ptr = indices.Ptr;
        for (var i = 0; i < length; i++)
        {
            var face = faces[i];
            *ptr++ = face.MIndices[0];
            *ptr++ = face.MIndices[1];
            *ptr++ = face.MIndices[2];
        }
    }

    private static void WriteIndicesU16(AssimpMesh* mesh, NativeView<ushort> indices)
    {
        var length = mesh->MNumFaces;
        var faces = mesh->MFaces;
        var ptr = indices.Ptr;
        for (var i = 0; i < length; i++)
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
        MeshImportContext ctx,
        NativeView<VertexShading> vertices)
    {
        var count = (int)aiMesh->MNumVertices;
        ArgumentOutOfRangeException.ThrowIfLessThan(vertices.Length, count, nameof(vertices.Length));

        var meshEntry = ctx.Meshes[meshIndex];
/*
        var bounds = BoundingBox.Infinite;
        for (int i = 0; i < count; i++)
        {
            bounds.FromPoint(aiMesh->MVertices[i]);
        }
*/
        BoundingBox.FromPoints(new ReadOnlySpan<Vector3>(aiMesh->MVertices, count), out var bounds);
        meshEntry.SetBounds(in bounds);

        var texCoords = aiMesh->MTextureCoords[0];
        for (int i = 0; i < count; i++)
        {
            ref var v = ref vertices[i];
            v.TexCoords = texCoords[i].AsVector2();
            v.Normal = aiMesh->MNormals[i];
            v.Tangent = aiMesh->MTangents[i];
        }
    }

    private static void WriteSkinningData(AssimpMesh* aMesh, ModelImportContext ctx,
        NativeView<SkinningData> skinningData)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)aMesh->MNumBones, AssimpUtils.BoneLimit);

        // clear
        skinningData.Reinterpret<byte>().AsSpan().Fill(byte.MaxValue);

        // write
        {
            var boneLen = aMesh->MNumBones;
            var bones = aMesh->MBones;
            var inverseBindPose = ctx.AnimationContext.InverseBindPose;
            for (var i = 0; i < boneLen; i++)
            {
                var bone = bones[i];
                ctx.TryGetBoneIndex(AssimpUtils.GetNameHash(bone->MName), out var boneIndex);
                inverseBindPose[boneIndex] = bone->MOffsetMatrix;

                WriteWeightAndIndices(bone, (byte)boneIndex, skinningData);
            }
        }

        // sanitize
        for (var i = 0; i < skinningData.Length; i++)
        {
            ref var data = ref skinningData[i];
            if (data.I0 == byte.MaxValue)
            {
                data = default;
            }
            else if (data.I1 == byte.MaxValue)
            {
                data.I1 = 0;
                data.I2 = 0;
                data.I3 = 0;

                data.W1 = 0;
                data.W2 = 0;
                data.W3 = 0;
            }
            else if (data.I2 == byte.MaxValue)
            {
                data.I2 = 0;
                data.I3 = 0;

                data.W2 = 0;
                data.W3 = 0;
            }
            else if (data.I3 == byte.MaxValue)
            {
                data.I3 = 0;
                data.W3 = 0;
            }
        }
    }

    private static void WriteWeightAndIndices(Bone* bone, byte boneIndex, NativeView<SkinningData> skinningData)
    {
        var weightLen = bone->MNumWeights;
        var weights = bone->MWeights;
        for (var j = 0; j < weightLen; j++)
        {
            var weight = weights[j];
            var vertexId = (int)weight.MVertexId;
            if (vertexId >= skinningData.Length) continue;

            ref var data = ref skinningData[vertexId];
            var packedWeight = (byte)float.Clamp(float.Round(weight.MWeight * 255f), 0f, 255f);

            if (data.I0 == byte.MaxValue)
            {
                data.I0 = boneIndex;
                data.W0 = packedWeight;
            }
            else if (data.I1 == byte.MaxValue)
            {
                data.I1 = boneIndex;
                data.W1 = packedWeight;
            }
            else if (data.I2 == byte.MaxValue)
            {
                data.I2 = boneIndex;
                data.W2 = packedWeight;
            }
            else if (data.I3 == byte.MaxValue)
            {
                data.I3 = boneIndex;
                data.W3 = packedWeight;
            }
        }
    }
}