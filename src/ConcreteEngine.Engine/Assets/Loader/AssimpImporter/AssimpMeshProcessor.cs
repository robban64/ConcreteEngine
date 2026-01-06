using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Assets.Loader.State;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace ConcreteEngine.Engine.Assets.Loader.AssimpImporter;

internal sealed class AssimpMeshProcessor(ModelLoaderDataTable dataTable, ModelLoaderState state)
{
    public unsafe MeshCreationInfo ProcessAndUploadMeshes(AssimpMesh* mesh, in Matrix4x4 world, int meshIndex,
        AssetGfxUploader gfxUploader,
        out BoundingBox bounds)
    {
        if (mesh->MNumBones > 0)
            WriteSkinningData(mesh);

        var info = LoadAndUploadMesh(mesh, world, gfxUploader, state.HasAnimationChannels, out bounds);
        state.AppendMeshInfo(mesh->MName.AsString, meshIndex, info);
        return info;
    }


    private unsafe MeshCreationInfo LoadAndUploadMesh(AssimpMesh* mesh, in Matrix4x4 world,
        AssetGfxUploader gfxUploader, bool isAnimated,
        out BoundingBox bounds)
    {
        var vertexCount = (int)mesh->MNumVertices;
        var indexCount = (int)(mesh->MNumFaces * 3);

        var info = new MeshCreationInfo();

        if (!isAnimated)
        {
            var writer = dataTable.WriteVertex(vertexCount, indexCount);
            WriteIndices(mesh, writer.Indices);
            WriteVertices(mesh, in world, writer.Vertices, out bounds);
            gfxUploader.UploadMesh(dataTable.GetUploadData(vertexCount, indexCount, ref info));
        }
        else
        {
            var writer = dataTable.WriteVertexSkinned(vertexCount, indexCount);
            WriteIndices(mesh, writer.Indices);
            WriteVerticesSkinned(mesh, in world, writer.Vertices, writer.Skinned, out bounds);
            gfxUploader.UploadMesh(dataTable.GetSkinnedUploadData(vertexCount, indexCount, ref info));
        }

        return info;
    }


    private unsafe void WriteSkinningData(AssimpMesh* mesh)
    {
        int vertexCount = (int)mesh->MNumVertices, boneCount = (int)mesh->MNumBones;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(boneCount, ImportModelUtils.BoneTransformsCapacity);

        //ensure capacity for skinningData
        dataTable.WriteSkinningData(vertexCount, out var skinningData, out var boneTransforms);
        dataTable.FillDefaultSkinningData(vertexCount);
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

    private static unsafe void WriteVertices(AssimpMesh* mesh, in Matrix4x4 world, Span<Vertex3D> vertices,
        out BoundingBox bounds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(vertices.Length, (int)mesh->MNumVertices, nameof(vertices.Length));

        var count = mesh->MNumVertices;
        if (count > vertices.Length || count > mesh->MNumVertices)
            throw new IndexOutOfRangeException();


        bounds = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));
        for (int i = 0; i < count; i++)
        {
            ref var v = ref vertices[i];
            v.Position = Vector3.Transform(mesh->MVertices[i], world);
            v.Normal = mesh->MNormals[i];
            v.Tangent = mesh->MTangents[i];
            v.TexCoords = mesh->MTextureCoords[0][i].ToVec2();

            bounds.FromPoint(v.Position);
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

    private static unsafe void WriteVerticesSkinned(AssimpMesh* mesh, in Matrix4x4 world, Span<Vertex3DSkinned> result,
        ReadOnlySpan<SkinningData> skinned, out BoundingBox bounds)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Length, skinned.Length, nameof(result.Length));

        var count = mesh->MNumVertices;
        if (count > result.Length || count > skinned.Length)
            throw new IndexOutOfRangeException();

        bounds = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));

        for (int i = 0; i < count; i++)
        {
            ref readonly var skinnedVertex = ref skinned[i];
            ref var v = ref result[i];
            //TODO transform vertices
            v.Position = mesh->MVertices[i];
            v.Normal = mesh->MNormals[i];
            v.Tangent = mesh->MTangents[i];
            v.TexCoords = mesh->MTextureCoords[0][i].ToVec2();
            v.BoneIndices = skinnedVertex.BoneIndices;
            v.BoneWeights = skinnedVertex.BoneWeights;

            bounds.FromPoint(Vector3.Transform(v.Position, world));
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