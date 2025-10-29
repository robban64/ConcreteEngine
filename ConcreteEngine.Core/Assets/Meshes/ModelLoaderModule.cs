#region

using System.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.Internal;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Core.Assets.Meshes;

internal sealed class ModelLoaderModule
{
    private VertexAttribute[] DefaultAttribs { get; }

    private MeshLoader _loader;
    private readonly AssetGfxUploader _uploader;

    public ModelLoaderModule(AssetGfxUploader uploader)
    {
        _uploader = uploader;
        _loader = new MeshLoader(OnProcess);

        var attribBuilder = new VertexAttributeMaker<Vertex3D>();
        DefaultAttribs =
        [
            attribBuilder.Make<Vector3>(0),
            attribBuilder.Make<Vector2>(1),
            attribBuilder.Make<Vector3>(2),
            attribBuilder.Make<Vector3>(3)
        ];
    }

    public Model LoadModel(AssetId assetId, MeshDescriptor manifest, out AssetFileSpec[] fileSpecs)
    {
        var refId = AssetRef<Model>.Make(assetId);

        _loader.LoadMesh(manifest, out fileSpecs, out var meshesInfo);

        var meshParts = new ModelMesh[meshesInfo.Length];
        var drawCount = 0;
        for (int i = 0; i < meshesInfo.Length; i++)
        {
            var info = meshesInfo[i];
            var meshInfo = info.Info;
            meshParts[i] = new ModelMesh(refId, info.Name, meshInfo.MeshId, info.MaterialSlot,
                meshInfo.DrawCount, info.Transform);

            drawCount += info.Info.DrawCount;
        }

        return new Model
        {
            RawId = assetId,
            Name = manifest.Name,
            MeshParts = meshParts,
            DrawCount = drawCount,
            IsCoreAsset = false
        };
    }

    public void Unload()
    {
        _loader.ClearCache();
        _loader = null!;
    }

    private MeshCreationInfo OnProcess(MeshImportData data)
    {
        var payload = new MeshUploadPayload(
            attributes: DefaultAttribs,
            vertices: data.Vertices,
            indices: data.Indices,
            properties: new MeshDrawProperties(
                Kind: DrawMeshKind.Elements,
                DrawCount: data.Indices.Length,
                ElementSize: DrawElementSize.UnsignedInt,
                Primitive: DrawPrimitive.Triangles
            )
        );

        return _uploader.UploadMesh(payload);
    }
}