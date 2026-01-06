using ConcreteEngine.Core.Common;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.AssimpImporter;
using ConcreteEngine.Engine.Assets.Loader.State;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Metadata.Asset;

namespace ConcreteEngine.Engine.Assets.Loader.Importer;

internal sealed class ModelImporter
{
    private ModelAssimpImporter _modelAssimpImporter;
    private ModelLoaderDataTable _dataTable;
    private ModelLoaderState _state;

    public ModelImporter(AssetGfxUploader uploader, ModelLoaderState state)
    {
        _state = state;
        _dataTable = new ModelLoaderDataTable();
        _modelAssimpImporter = new ModelAssimpImporter(uploader, _dataTable, _state);
    }

    public ModelLoaderResult LoadMesh(AssetId id, string name, string fileName)
    {
        var path = Path.Combine(EnginePath.ModelPath, fileName);

        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

        _state.Start(name, fileName);
        _dataTable.Clear();

        //
        _modelAssimpImporter.ImportMesh(path);
        //

        InvalidOpThrower.ThrowIf(_state.MeshCount == 0);

        var drawCount = 0;
        var meshParts = new ModelMesh[_state.MeshCount];
        var meshData = _dataTable.GetMeshDataResult(meshParts.Length);
        for (int i = 0; i < meshParts.Length; i++)
        {
            ref readonly var part = ref meshData.Parts[i];
            var meshInfo = part.CreationInfo;
            var partName = _state.GetMeshName(i);
            meshParts[i] = new ModelMesh(new AssetRef<Model>(id), partName, meshInfo.MeshId, part.MaterialSlot,
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
                _dataTable.BoneOffsetMatrix,
                _dataTable.NodeTransforms,
                in _dataTable.InvRootTransform,
                in _dataTable.SkeletonRootOffset);
        }

        return _state.BuildResult(fi.Length, meshParts, animation, drawCount, in meshData.Bounds);
    }


    public void Teardown()
    {
        _dataTable.Teardown();
        _dataTable = null!;

        _state.Clear();
        _state = null!;

        _modelAssimpImporter.Teardown();
        _modelAssimpImporter = null!;

        Console.WriteLine("Teardown complete.");
    }
}