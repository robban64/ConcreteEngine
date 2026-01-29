
using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Common.Numerics.Primitives;
using ConcreteEngine.Engine.Assets.Loader.AssimpImporter;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace ConcreteEngine.Engine.Assets.Loader.ModelImporterV2;

internal sealed unsafe partial class ModelImporter
{
    private static void WriteIndices(AssimpMesh* mesh, Span<uint> indices)
    {
        var idx = 0;
        for (int i = 0; i < mesh->MNumFaces; i++)
        {
            var face = mesh->MFaces[i];
            indices[idx++] = face.MIndices[0];
            indices[idx++] = face.MIndices[1];
            indices[idx++] = face.MIndices[2];
        }
    }

    private static void WriteVertices(AssimpMesh* mesh, MeshEntry meshEntry, Span<Vertex3D> vertices)
    {
        var count = (int)mesh->MNumVertices;
        ArgumentOutOfRangeException.ThrowIfLessThan(vertices.Length, count, nameof(vertices.Length));

        ref readonly var transform = ref Ctx.Model.WorldTransforms[meshEntry.Info.MeshIndex];
        var bounds = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));
        for (int i = 0; i < count; i++)
        {
            ref var v = ref vertices[i];
            v.Position = Vector3.Transform(mesh->MVertices[i], transform);
            v.Normal = mesh->MNormals[i];
            v.Tangent = mesh->MTangents[i];
            v.TexCoords = mesh->MTextureCoords[0][i].ToVec2();

            bounds.FromPoint(v.Position);
        }

        meshEntry.LocalBounds = bounds;
    }

    private static void WriteVerticesSkinned(AssimpMesh* aMesh, MeshEntry meshEntry, Span<VertexSkinned> vertices)
    {
        var count = aMesh->MNumVertices;
        if (count > vertices.Length)
            throw new IndexOutOfRangeException();

        ref readonly var transform = ref Ctx.Model.WorldTransforms[meshEntry.Info.MeshIndex];
        var bounds = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));
        for (int i = 0; i < count; i++)
        {
            ref var v = ref vertices[i];
            //TODO transform vertices
            v.Position = aMesh->MVertices[i];
            v.Normal = aMesh->MNormals[i];
            v.Tangent = aMesh->MTangents[i];
            v.TexCoords = aMesh->MTextureCoords[0][i].ToVec2();

            //ref readonly var skinnedVertex = ref skinned[i];
            //v.BoneIndices = skinnedVertex.BoneIndices;
            //v.BoneWeights = skinnedVertex.BoneWeights;

            bounds.FromPoint(Vector3.Transform(v.Position, transform));
        }

        meshEntry.LocalBounds = bounds;
    }


    private static void WriteSkinningData(AssimpMesh* aMesh, MeshEntry mesh, Span<VertexSkinned> vertices)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)aMesh->MNumBones, AssimpUtils.BoneLimit);

        // Clear
        for (int i = 0; i < vertices.Length; i++)
        {
            ref var skinned = ref vertices[i];
            skinned.BoneIndices = new Int4(-1, -1, -1, -1);
            skinned.BoneWeights = default;
        }

        var meshIndex = mesh.Info.MeshIndex;
        for (var i = 0; i < aMesh->MNumBones; i++)
        {
            var bone = aMesh->MBones[i];
            var boneIndex = Ctx.BoneIndexByMeshBone[(meshIndex, i)];
            WriteWeightAndIndices(bone, boneIndex, vertices);
        }

        // sanitize
        foreach (ref var data in vertices)
        {
            if (data.BoneIndices.X < 0) data.BoneIndices.X = 0;
            if (data.BoneIndices.Y < 0) data.BoneIndices.Y = 0;
            if (data.BoneIndices.Z < 0) data.BoneIndices.Z = 0;
            if (data.BoneIndices.W < 0) data.BoneIndices.W = 0;
        }
    }


    private static void WriteWeightAndIndices(Bone* bone, int boneIndex, Span<VertexSkinned> skinningData)
    {
        for (uint j = 0; j < bone->MNumWeights; j++)
        {
            var weight = bone->MWeights[j];

            if (weight.MVertexId >= skinningData.Length) continue;

            ref var data = ref skinningData[(int)weight.MVertexId];

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