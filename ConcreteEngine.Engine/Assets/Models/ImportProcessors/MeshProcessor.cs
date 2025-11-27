#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Extensions;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Models.Loader;
using ConcreteEngine.Graphics.Primitives;
using AssimpMesh = Silk.NET.Assimp.Mesh;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.ImportProcessors;

internal sealed class MeshProcessor(ModelImportDataStore dataStore)
{
    public unsafe MeshCreationInfo LoadAndUploadMesh(AssimpMesh* mesh, AssetGfxUploader gfxUploader, bool isAnimated)
    {
        var vertexCount = (int)mesh->MNumVertices;
        var indexCount = (int)(mesh->MNumFaces * 3);

        var info = new MeshCreationInfo();

        if (!isAnimated)
        {
            var writer = dataStore.WriteVertex(vertexCount, indexCount);
            WriteIndices(mesh, writer.Indices);
            WriteVertices(mesh, writer.Vertices);
            gfxUploader.UploadMesh(dataStore.GetUploadData(vertexCount, indexCount, ref info));
        }
        else
        {
            var writer = dataStore.WriteVertexSkinned(vertexCount, indexCount);
            WriteIndices(mesh, writer.Indices);
            WriteVerticesSkinned(mesh, writer.Vertices, writer.Skinned);
            gfxUploader.UploadMesh(dataStore.GetSkinnedUploadData(vertexCount, indexCount, ref info));
        }

        return info;
    }


    private static unsafe void WriteVertices(AssimpMesh* mesh, Span<Vertex3D> vertices)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(vertices.Length, (int)mesh->MNumVertices, nameof(vertices.Length));

        var count = mesh->MNumVertices;
        for (int i = 0; i < count; i++)
        {
            ref var v = ref vertices[i];
            v.Position = mesh->MVertices[i];
            v.Normal = mesh->MNormals[i];
            v.Tangent = mesh->MTangents[i];
            v.TexCoords = mesh->MTextureCoords[0][i].ToVec2();
        }
    }

    private static unsafe void WriteIndices(AssimpMesh* mesh, Span<uint> indices)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(indices.Length, (int)mesh->MNumFaces, nameof(indices.Length));

        var idx = 0;
        for (int i = 0; i < mesh->MNumFaces; i++)
        {
            var face = mesh->MFaces[i];
            indices[idx++] = face.MIndices[0];
            indices[idx++] = face.MIndices[1];
            indices[idx++] = face.MIndices[2];
        }
    }

    private static unsafe void WriteVerticesSkinned(AssimpMesh* mesh, Span<Vertex3DSkinned> result, ReadOnlySpan<SkinningData> skinned)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Length, skinned.Length, nameof(result.Length));

        var count = mesh->MNumVertices;
        if (count > result.Length || count > skinned.Length)
            throw new IndexOutOfRangeException();

        for (int i = 0; i < count; i++)
        {
            ref readonly var skinnedVertex = ref skinned[i];
            ref var v = ref result[i];

            v.Position = mesh->MVertices[i];//ImportUtils.TransformZupToYup(mesh->MVertices[i]);
            v.Normal = mesh->MNormals[i];//ImportUtils.TransformZupToYup(mesh->MNormals[i]);
            v.Tangent = mesh->MTangents[i];
            v.TexCoords = mesh->MTextureCoords[0][i].ToVec2();
            v.BoneIndices = skinnedVertex.BoneIndices;
            v.BoneWeights = skinnedVertex.BoneWeights;
        }
    }


}