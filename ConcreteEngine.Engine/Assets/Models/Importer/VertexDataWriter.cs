using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics.Extensions;
using ConcreteEngine.Engine.Assets.Models.Loader;
using ConcreteEngine.Graphics.Primitives;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace ConcreteEngine.Engine.Assets.Models.Importer;

internal static unsafe class VertexDataWriter
{
    internal static void WriteVertices(AssimpMesh* mesh, Span<Vertex3D> vertices)
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

    internal static void WriteIndices(AssimpMesh* mesh, Span<uint> indices)
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

    internal static void WriteVerticesSkinned(AssimpMesh* mesh, Span<Vertex3DSkinned> result,
        ReadOnlySpan<SkinningData> skinned)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Length, skinned.Length, nameof(result.Length));

        var count = mesh->MNumVertices;
        if (count > result.Length || count > skinned.Length)
            throw new IndexOutOfRangeException();
        
        for (int i = 0; i < count; i++)
        {
            ref readonly var skinnedVertex = ref skinned[i];
            ref var v = ref result[i];

            v.Position = mesh->MVertices[i];
            v.Normal = mesh->MNormals[i];
            v.Tangent = mesh->MTangents[i];
            v.TexCoords = mesh->MTextureCoords[0][i].ToVec2();
            v.BoneIndices = skinnedVertex.BoneIndices;
            v.BoneWeights = skinnedVertex.BoneWeights;
        }
    }
}