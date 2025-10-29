#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.IO;

#endregion

namespace ConcreteEngine.Core.Assets.Meshes;

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
                storage: AssetStorageKind.FileSystem,
                logicalName: record.Name,
                relativePath: record.Filename,
                sizeBytes: fi.Length
            )
        ];
    }


    public void ClearCache()
    {
        _meshImporter.ClearCache();
    }
}