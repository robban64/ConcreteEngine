#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
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

    public AssetFileSpec[] LoadMesh(MeshDescriptor record, out Span<MeshImportResult> parts, out Span<Matrix4x4> partTransforms, out ModelImportResult modelResult)
    {
        var path = AssetPaths.GetMeshPath(record.Filename);

        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

        modelResult = _meshImporter.ImportMesh(path, out parts, out partTransforms);

        return
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