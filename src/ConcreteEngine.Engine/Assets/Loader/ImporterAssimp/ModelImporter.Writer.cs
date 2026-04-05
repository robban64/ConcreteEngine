using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Primitives;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;

internal sealed unsafe partial class ModelImporter
{
    private static void WriteIndices(AssimpMesh* mesh, NativeViewPtr<uint> indices)
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

    private static void WriteVertices(
        AssimpMesh* aiMesh,
        int meshIndex,
        ModelImportData model,
        NativeViewPtr<Vertex3D> vertices)
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

    private static void WriteVerticesSkinned(
        AssimpMesh* aiMesh,
        int meshIndex,
        ModelImportData model,
        NativeViewPtr<Vertex3D> vertices)
    {
        var count = (int)aiMesh->MNumVertices;
        ArgumentOutOfRangeException.ThrowIfLessThan(vertices.Length, count, nameof(vertices.Length));
        
        var meshEntry = model.Meshes[meshIndex];
        ref readonly var transform = ref meshEntry.WorldTransform;
        var bounds = BoundingBox.Infinite;
        var texCoords = aiMesh->MTextureCoords[0];
        for (int i = 0; i < count; i++)
        {
            ref var v = ref vertices[i];
            v.Position = aiMesh->MVertices[i];
            v.TexCoords = texCoords[i].AsVector2();
            v.Normal = aiMesh->MNormals[i];
            v.Tangent = aiMesh->MTangents[i];
            bounds.FromPoint(Vector3.Transform(v.Position, transform));
        }

        meshEntry.LocalBounds = bounds;
    }


    private static void WriteSkinningData(AssimpMesh* aMesh, ModelAnimation animation,
        NativeViewPtr<SkinningData> vertices)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)aMesh->MNumBones, AssimpUtils.BoneLimit);

        var len = vertices.Length;

        // clear
        for (var i = 0; i < len; i++)
        {
            ref var data = ref vertices[i];
            data.BoneIndices = Int4.NegativeOne;
            data.BoneWeights = default;
        }

        // write
        {
            var boneLen = aMesh->MNumBones;
            var bones = aMesh->MBones;
            var inverseBindPose = animation.Skeleton.InverseBindPose;
            for (var i = 0; i < boneLen; i++)
            {
                var bone = bones[i];
                TryGetBoneIndex(AssimpUtils.GetNameHash(bone->MName), out var boneIndex);
                inverseBindPose[boneIndex] = bone->MOffsetMatrix;

                WriteWeightAndIndices(bone, boneIndex, vertices);
            }
        }

        // sanitize
        for (var i = 0; i < len; i++)
        {
            ref var boneIndices = ref vertices[i].BoneIndices;
            boneIndices.X = int.Max(boneIndices.X, 0);
            boneIndices.Y = int.Max(boneIndices.Y, 0);
            boneIndices.Z = int.Max(boneIndices.Z, 0);
            boneIndices.W = int.Max(boneIndices.W, 0);
        }

    }

    private static void WriteWeightAndIndices(Bone* bone, int boneIndex, NativeViewPtr<SkinningData> skinningData)
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