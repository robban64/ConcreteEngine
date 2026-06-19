using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;
using static ConcreteEngine.Core.Engine.Assets.Utils.AssetKindUtils;

namespace ConcreteEngine.Engine.Assets;
/*
internal sealed class AssetScannerEntry
{
    public readonly AssetKind Kind;
    public readonly string ValidExt;
    public readonly string AssetExtension;

    private Queue<AssetRecord> _records;

    public AssetScannerEntry(AssetKind kind, string validExt, string assetExt = ".asset")
    {
        Kind = kind;
        ValidExt = validExt;
        AssetExtension = assetExt;
        _records = new Queue<AssetRecord>(kind == AssetKind.Shader ? 16 : 64);
    }
}
*/
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

        ScanFiles(result);
    }

    private void ScanFiles(Queue<AssetRecord>[] result)
    {
        const SearchOption searchOption = SearchOption.AllDirectories;

        var files = assetManager.Files;
        var relativeOffset = EnginePath.AssetBasePath.Length + 1;
        foreach (var filePath in Directory.EnumerateFiles(EnginePath.AssetBasePath, "*.*", searchOption))
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = fileInfo.Name;
            if(!FileUtils.TestFileName(fileName, out _, out var isAssetFile))
                continue;

            var relativePath = filePath.AsSpan(relativeOffset).ToString();

            if (files.HasFilePath(relativePath)) continue;

            FileScanInfo scanInfo;
            if (!isAssetFile)
            {
                ExtractFileInfo(0, fileName, relativePath, fileInfo, out scanInfo);
                files.RegisterFile(FileBinding.UnboundFile, in scanInfo);
                continue;
            }
            
            var record = AssetSerializer.LoadRecord(filePath);
            result[record.Kind.ToIndex()].Enqueue(record);
            ExtractFileInfo(0, record.Name, relativePath, fileInfo, out scanInfo);
                
            // Asset file
            var assetId = assetManager.RegisterScannedAsset(record, in scanInfo);
                
            // Dependent files
            RegisterBindings(assetId, record, filePath, Path.GetDirectoryName(relativePath.AsSpan()));
        }
    }
    

    private void RegisterBindings(AssetId assetId, AssetRecord record, string filePath, ReadOnlySpan<char> relativeDirectory)
    {
        var fileIndex = 1;
        var path = filePath.AsSpan(0, filePath.LastIndexOf('/'));
        foreach (var localPath in record.Files.Values)
        {
            var relativePath = Path.Join(relativeDirectory, localPath);
            var fileInfo = new FileInfo(Path.Join(path, localPath));
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