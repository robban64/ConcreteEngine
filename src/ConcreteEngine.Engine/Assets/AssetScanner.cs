using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;
using static ConcreteEngine.Core.Engine.Assets.Utils.AssetKindUtils;

namespace ConcreteEngine.Engine.Assets;

internal sealed class AssetScannerEntry
{
    public readonly AssetKind Kind;
    public readonly string Directory;
    public readonly string[] ValidExtensions;
    public readonly string AssetExtension;

    private Queue<AssetRecord> _records;

    public AssetScannerEntry(AssetKind kind, string directory, string[] validExt, string assetExt = "asset")
    {
        Kind = kind;
        Directory = directory;
        ValidExtensions = validExt;
        AssetExtension = assetExt;
        _records = new Queue<AssetRecord>(kind == AssetKind.Shader ? 16 : 64);
    }
}

internal sealed class AssetScanner(AssetManager assetManager)
{
    public void ScanAll(Queue<AssetRecord>[] result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Length, AssetTypeCount);

        var shaderQueue = result[AssetKind.Shader.ToIndex()] = new Queue<AssetRecord>(16);
        var textureQueue = result[AssetKind.Texture.ToIndex()] = new Queue<AssetRecord>(64);
        var modelQueue = result[AssetKind.Model.ToIndex()] = new Queue<AssetRecord>(64);
        var materialQueue = result[AssetKind.Material.ToIndex()] = new Queue<AssetRecord>(64);

        avg1.BeginSample();
        ScanFiles(AssetKind.Shader, EnginePath.ShaderPath,  shaderQueue);
        ScanFiles(AssetKind.Texture, EnginePath.TexturePath,  textureQueue);
        ScanFiles(AssetKind.Model, EnginePath.ModelPath,  modelQueue);
        ScanFiles(AssetKind.Material, EnginePath.MaterialPath, materialQueue);
        avg1.EndSample();
        avg1.ResetAndPrint("Scanner took");
    }

    private AvgFrameTimer avg1;

    private void ScanFiles(
        AssetKind kind,
        string directory,
        Queue<AssetRecord> result)
    {
        var fileRegistry = assetManager.Files;
        var relativeDirectory = directory.Substring(directory.LastIndexOf('/') + 1);

        // register files
        foreach (var filePath in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = fileInfo.Name;
            if(!FileUtils.TestFileName(kind, fileName, out var isAssetFile))
                continue;

            var relativeSpan = filePath.AsSpan();
            relativeSpan = relativeSpan.Slice(relativeSpan.LastIndexOf(directory) + directory.Length + 1);
            var relativePath = Path.Join(relativeDirectory, relativeSpan);

            if (fileRegistry.HasFilePath(relativePath)) continue;

            FileScanInfo scanInfo;
            if (isAssetFile)
            {
                var record = AssetSerializer.LoadRecord(filePath);
                ExtractFileInfo(0, record.Name, relativePath, fileInfo, out scanInfo);
                var assetId = assetManager.RegisterScannedAsset(record, in scanInfo);
                result.Enqueue(record);

                // Dependent files
                RegisterBindings(assetId, record, directory, relativeDirectory);
                continue;
            }
            ExtractFileInfo(0, fileName, relativePath, fileInfo, out scanInfo);
            fileRegistry.RegisterUnbound(in scanInfo);
        }
    }
    

    private void RegisterBindings(AssetId assetId, AssetRecord record, string directory, string relativeDirectory)
    {
        var fileIndex = 1;
        foreach (var (_, localPath) in record.Files)
        {
            var relativePath = Path.Join(relativeDirectory, localPath);
            var fileInfo = new FileInfo(Path.Join(directory, localPath));
            ExtractFileInfo(fileIndex++, fileInfo.Name, relativePath, fileInfo, out var info);
            assetManager.RegisterAssetBinding(assetId, in info);
        }
    }
    
    private static void ExtractFileInfo(int index, string name, string relativePath, FileInfo info, out FileScanInfo scanInfo)
    {
        //var expectedMagic = GetMagicBytesForPath(fullPath);
        //if (expectedMagic != null && !IsFileHeaderValid(fullPath, expectedMagic))
        //    return false;
        
        //TODO FIX
        if (!info.Exists)
            scanInfo = new FileScanInfo((byte)index, name, relativePath);
        else
            scanInfo = new FileScanInfo((byte)index, name, relativePath, info.Length, info.LastWriteTime);
    }

}