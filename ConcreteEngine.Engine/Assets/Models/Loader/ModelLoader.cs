#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
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
        public required ModelMaterialEmbeddedDescriptor[] MaterialEntries { get; init; }
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

        var materialEntries = _modelImporter.ImportMesh(path, out var modelResult, out var animationRes);

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
            Console.WriteLine("Model Name: " + name);
            foreach (var it in animationRes.Animations)
            {
                Console.WriteLine($"{it.Name}: {it.Duration} - {it.TicksPerSecond}");
            }

            animation = new ModelAnimation(
                animationRes.BoneMapping,
                animationRes.Animations,
                animationRes.ParentIndices,
                animationRes.BoneTransforms, 
                in animationRes.InvRootTransform);
        }


        fileSpec = [new AssetFileSpec(AssetStorageKind.FileSystem, name, fileName, fi.Length)];

        foreach (var mat in materialEntries)
        {
            mat.FileSpec = [new AssetFileSpec(AssetStorageKind.Embedded, mat.Name, fileName, 0)];
            foreach (var tex in mat.EmbeddedTextures.Values)
            {
                tex.FileSpec = [new AssetFileSpec(AssetStorageKind.Embedded, tex.Name, fileName, tex.PixelData.Length)];
            }
        }

        return new ModelLoaderResult
        {
            Animation = animation,
            MeshParts = meshParts,
            DrawCount = drawCount,
            Bounds = modelResult.Bounds,
            MaterialEntries = materialEntries
        };
    }


    public void ClearCache()
    {
        _modelImporter.ClearCache();
    }
}