#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.IO;
using ConcreteEngine.Engine.Assets.Models.Importer;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Loader;

internal sealed class ModelLoader
{
    public class ModelLoaderResult
    {
        public required ModelAnimation? Animation { get; init; }
        public required ModelMesh[] MeshParts { get; init; }
        public required int DrawCount { get; init; }
        public required BoundingBox Bounds { get; init; }
    }

    private readonly ModelImporter _modelImporter;

    public ModelLoader(AssetGfxUploader uploader)
    {
        _modelImporter = new ModelImporter(uploader);
    }

    public ModelLoaderResult LoadMesh(AssetRef<Model> refId, string name, string fileName, out AssetFileSpec[] fileSpec)
    {
        var path = AssetPaths.GetMeshPath(fileName);

        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

        _modelImporter.ImportMesh(path, out var modelResult, out var animationRes);

        var drawCount = 0;
        var meshParts = new ModelMesh[modelResult.Count];
        for (int i = 0; i < meshParts.Length; i++)
        {
            ref readonly var part = ref modelResult.Parts[i];

            var meshInfo = part.CreationInfo;
            meshParts[i] = new ModelMesh(refId, modelResult.PartNames[i], meshInfo.MeshId, part.MaterialSlot,
                meshInfo.DrawCount, in modelResult.PartTransforms[i], in part.Bounds);

            drawCount += meshInfo.DrawCount;
        }


        ModelAnimation? animation = null;
        if (animationRes.BoneTransforms.Length > 0 && animationRes.BoneMapping?.Count > 0)
        {
            animation = new ModelAnimation(animationRes.BoneMapping.ToDictionary(),
                animationRes.BoneTransforms.ToArray(), in animationRes.InvRootTransform);
        }


        fileSpec =
        [
            new AssetFileSpec(
                Storage: AssetStorageKind.FileSystem,
                LogicalName: name,
                RelativePath: fileName,
                SizeBytes: fi.Length
            )
        ];

        return new ModelLoaderResult
        {
            Animation = animation, MeshParts = meshParts, DrawCount = drawCount, Bounds = modelResult.Bounds
        };
    }


    public void ClearCache()
    {
        _modelImporter.ClearCache();
    }
}