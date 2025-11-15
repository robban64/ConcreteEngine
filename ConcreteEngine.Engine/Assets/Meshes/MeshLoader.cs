#region

using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.IO;

#endregion

namespace ConcreteEngine.Engine.Assets.Meshes;

internal sealed class MeshLoader
{
    private readonly MeshImporter _meshImporter;

    public MeshLoader(Func<MeshImportData, MeshCreationInfo> onProcess)
    {
        _meshImporter = new MeshImporter(onProcess);
    }

    public void LoadMesh(
        MeshDescriptor record,
        out AssetFileSpec[] fileSpec,
        out ReadOnlySpan<MeshPartImportResult> infos)
    {
        var path = AssetPaths.GetMeshPath(record.Filename);

        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

        infos = _meshImporter.ImportMesh(path);
        fileSpec =
        [
            new AssetFileSpec(
                Storage: AssetStorageKind.FileSystem,
                LogicalName: record.Name,
                RelativePath: record.Filename,
                SizeBytes: fi.Length
            )
        ];
    }


    public void ClearCache()
    {
        _meshImporter.ClearCache();
    }
}