#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Engine.Assets.Meshes;

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

    public Model LoadModel(AssetId assetId, MeshDescriptor manifest, bool isCoreAsset, out AssetFileSpec[] fileSpecs)
    {
        var refId = AssetRef<Model>.Make(assetId);

        fileSpecs = _loader.LoadMesh(manifest, out var modelResult);

        var meshParts = new ModelMesh[modelResult.Count];
        var drawCount = 0;
        for (int i = 0; i < meshParts.Length; i++)
        {
            ref readonly var part = ref modelResult.Parts[i];

            var meshInfo = part.CreationInfo;
            meshParts[i] = new ModelMesh(refId, modelResult.PartNames[i], meshInfo.MeshId, part.MaterialSlot,
                meshInfo.DrawCount, in modelResult.PartTransforms[i], in part.Bounds);

            drawCount += part.CreationInfo.DrawCount;
        }

        return new Model
        {
            RawId = assetId,
            Name = manifest.Name,
            MeshParts = meshParts,
            DrawCount = drawCount,
            IsCoreAsset = isCoreAsset,
            Bounds = modelResult.Bounds
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
                kind: DrawMeshKind.Elements,
                drawCount: data.Indices.Length,
                elementSize: DrawElementSize.UnsignedInt,
                primitive: DrawPrimitive.Triangles
            )
        );

        return _uploader.UploadMesh(in payload);
    }
}