#region

using ConcreteEngine.Common;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.IO;
using ConcreteEngine.Engine.Assets.Models.ImportProcessors;

#endregion

namespace ConcreteEngine.Engine.Assets.Models.Loader;

internal sealed class ModelLoader
{
    private ModelImporter _modelImporter;
    private ModelImportDataStore _dataStore;
    private ModelLoaderState _state;

    public ModelLoader(AssetGfxUploader uploader, ModelLoaderState state)
    {
        _state = state;
        _dataStore = new ModelImportDataStore();
        _modelImporter = new ModelImporter(uploader, _dataStore, _state);
    }

    public ModelLoaderResult LoadMesh(AssetRef<Model> refId, string name, string fileName, out AssetFileSpec[] fileSpec)
    {
        var path = AssetPaths.GetMeshPath(fileName);

        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

        _state.Start(name, fileName);
        _dataStore.Clear();
        _modelImporter.ImportMesh(path);

        InvalidOpThrower.ThrowIf(_state.MeshCount == 0);

        var drawCount = 0;
        var meshParts = new ModelMesh[_state.MeshCount];
        var meshData = _dataStore.GetMeshDataResult(meshParts.Length);
        for (int i = 0; i < meshParts.Length; i++)
        {
            ref readonly var part = ref meshData.Parts[i];
            var meshInfo = part.CreationInfo;
            var partName = _state.GetMeshName(i);
            meshParts[i] = new ModelMesh(refId, partName, meshInfo.MeshId, part.MaterialSlot,
                meshInfo.DrawCount, in meshData.PartTransforms[i], in part.Bounds);

            drawCount += meshInfo.DrawCount;
        }


        ModelAnimation? animation = null;
        if (_state.IsAnimated)
        {
            _state.GetAnimationResult(out var boneMapping, out var animations, out var parentIndices);
            animation = new ModelAnimation(
                boneMapping,
                animations,
                parentIndices,
                _dataStore.BoneTransforms,
                _dataStore.NodeTransforms,
                in _dataStore.InvRootTransform,
                in _dataStore.SkeletonRootOffset);

        }

        fileSpec = [new AssetFileSpec(AssetStorageKind.FileSystem, name, fileName, fi.Length)];
        return _state.BuildResult(meshParts, animation, drawCount, in meshData.Bounds);
    }


    public void Teardown()
    {
        _modelImporter.Teardown();
        _dataStore.Teardown();

        _modelImporter = null!;
        _dataStore = null!;
        _state = null!;
    }
}