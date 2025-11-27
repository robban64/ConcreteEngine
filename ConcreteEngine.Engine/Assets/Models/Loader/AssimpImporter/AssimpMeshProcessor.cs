#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Extensions;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Loader.AssimpImporter;

internal sealed class AssimpMeshProcessor(ModelLoaderDataTable dataTable, ModelLoaderState state)
{

    public unsafe void ProcessAndUploadMeshes(AssimpMesh* mesh, int meshIndex, AssetGfxUploader gfxUploader,
        out MeshCreationInfo info)
    {
        if (mesh->MNumBones > 0)
            WriteSkinningData(mesh);

        info = LoadAndUploadMesh(mesh, gfxUploader, state.MightBeAnimated);
        state.AppendMeshInfo(mesh->MName.AsString, meshIndex, info);
    }

    public unsafe void BuildSkeletonHierarchy(AssimpNode* node)
    {
        var nodeName = node->MName.AsString;

        if (state.TryGetBoneIndex(nodeName, out int boneIndex))
        {
            if (node->MParent != null)
            {
                var parentName = node->MParent->MName.AsString;
                state.UpdateBoneParentIndexOrDefault(parentName, boneIndex);
            }

            if (boneIndex == 0)
            {
                var offset = Matrix4x4.Identity;
                var current = node->MParent;
                while (current != null)
                {
                    offset = current->MTransformation * offset;
                    current = current->MParent;
                }

                dataTable.SkeletonRootOffset = offset;
            }
        }

        //  check children
        for (uint i = 0; i < node->MNumChildren; i++)
            BuildSkeletonHierarchy(node->MChildren[i]);
    }

    private unsafe void WriteSkinningData(AssimpMesh* mesh)
    {
        int vertexCount = (int)mesh->MNumVertices, boneCount = (int)mesh->MNumBones;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(boneCount, ImportModelUtils.BoneTransformsCapacity);

        //ensure capacity for skinningData
        dataTable.WriteSkinningData(vertexCount, out var skinningData, out var boneTransforms);
        dataTable.FillDefaultSkinningData();
        var slicedSkinned = skinningData.Slice(0, vertexCount);

        for (var i = 0; i < mesh->MNumBones; i++)
        {
            ref var bone = ref mesh->MBones[i];
            var name = bone->MName.AsString;

            if (state.TryGetBoneIndex(name, out var boneIndex))
            {
                boneTransforms[boneIndex] = bone->MOffsetMatrix;
                WriteWeightAndIndices(bone, boneIndex, slicedSkinned);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        // sanitize
        SanitizeSkinningData(vertexCount, slicedSkinned);
    }

    private unsafe MeshCreationInfo LoadAndUploadMesh(AssimpMesh* mesh, AssetGfxUploader gfxUploader, bool isAnimated)
    {
        var vertexCount = (int)mesh->MNumVertices;
        var indexCount = (int)(mesh->MNumFaces * 3);

        var info = new MeshCreationInfo();

        if (!isAnimated)
        {
            var writer = dataTable.WriteVertex(vertexCount, indexCount);
            WriteIndices(mesh, writer.Indices);
            WriteVertices(mesh, writer.Vertices);
            gfxUploader.UploadMesh(dataTable.GetUploadData(vertexCount, indexCount, ref info));
        }
        else
        {
            var writer = dataTable.WriteVertexSkinned(vertexCount, indexCount);
            WriteIndices(mesh, writer.Indices);
            WriteVerticesSkinned(mesh, writer.Vertices, writer.Skinned);
            gfxUploader.UploadMesh(dataTable.GetSkinnedUploadData(vertexCount, indexCount, ref info));
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

    private static unsafe void WriteVerticesSkinned(AssimpMesh* mesh, Span<Vertex3DSkinned> result,
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


    private static unsafe void WriteWeightAndIndices(Bone* bone, int boneIndex, Span<SkinningData> skinningData)
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
        /*
            for (var j = 0; j < 4; j++)
            {
              var weight = bone->MWeights[j];
              if (weight.MVertexId >= skinningData.Length) continue;
              ref var data = ref skinningData[(int)weight.MVertexId];
              if (data.GetVertexId(j) < 0)
              {
                  data.Set(j, boneIndex, weight.MWeight);
                  break;
              }
            }
            return;
        */
    }


    private static void SanitizeSkinningData(int vertexCount, Span<SkinningData> skinningData)
    {
        for (int i = 0; i < vertexCount; i++)
        {
            ref var data = ref skinningData[i];
            if (data.BoneIndices.X < 0) data.BoneIndices.X = 0;
            if (data.BoneIndices.Y < 0) data.BoneIndices.Y = 0;
            if (data.BoneIndices.Z < 0) data.BoneIndices.Z = 0;
            if (data.BoneIndices.W < 0) data.BoneIndices.W = 0;
        }
    }
}