using System.IO.Enumeration;
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
        
        avg1.BeginSample();
        ScanAllFiles();
        ScanAllAssets(result);
        //ScanFiles(result);
        avg1.EndSample();
        avg1.ResetAndPrint("Scanner took");

    }
    private AvgFrameTimer avg1;

    private void ScanAllFiles()
    {
        var enumeration = new FileSystemEnumerable<FileScanInfo>(
            directory: EnginePath.AssetBasePath,
            transform: Transform, 
            options: new EnumerationOptions() { RecurseSubdirectories = true, }
            )
        {
            ShouldIncludePredicate = static (ref entry) =>
            {
                if(entry.IsDirectory || entry.IsHidden) return false;
                if (AssetManager.FileRegistry.HasFilePath(entry.ToSpecifiedFullPath())) return false;
                if(!FileUtils.TestFileName2(entry.FileName, out var kind) || kind == AssetKind.Unknown) return false;
                return true;
            },
        };

        foreach (var scanInfo in enumeration)
        {
            AssetManager.FileRegistry.RegisterFile(FileBinding.UnboundFile, scanInfo.Name, in scanInfo);
        }

        return;
        static FileScanInfo Transform(ref FileSystemEntry entry)
        {
            return new FileScanInfo(0, entry.FileName.ToString(), entry.ToSpecifiedFullPath(), entry.Length, entry.LastWriteTimeUtc.DateTime);
        }
    }
    
    private void ScanAllAssets(Queue<AssetRecord>[] result)
    {
        var enumeration = new FileSystemEnumerable<FileScanInfo>(
            directory: EnginePath.AssetBasePath,
            transform: Transform, 
            options: new EnumerationOptions() { RecurseSubdirectories = true, }
        )
        {
            ShouldIncludePredicate = static (ref entry) =>
            {
                if(entry.IsDirectory || entry.IsHidden || Path.GetExtension(entry.FileName) is not ".asset") 
                    return false;
                
                return !AssetManager.FileRegistry.HasFilePath(entry.ToSpecifiedFullPath());
            },
        };
        
        foreach (var scanInfo in enumeration)
        {
            var record = AssetSerializer.LoadRecord(scanInfo.RelativePath);
            var assetId = AssetManager.Instance.RegisterScannedAsset(record, in scanInfo);
            result[record.Kind.ToIndex()].Enqueue(record);

            var fileIndex = 1;
            var path = scanInfo.RelativePath.AsSpan(0, scanInfo.RelativePath.LastIndexOf('/'));
            foreach (var localPath in record.Files.Values)
            {
                var filePath = Path.Join(path, localPath);
                assetManager.RegisterAssetBinding(fileIndex++, assetId, record.Kind, filePath);
            }
        }

        return;
        static FileScanInfo Transform(ref FileSystemEntry entry)
        {
            var filePath = entry.ToSpecifiedFullPath();
            return new FileScanInfo(0, string.Empty, filePath, entry.Length, entry.LastWriteTimeUtc.DateTime);
        }
    }

/*
    private void ScanFiles(Queue<AssetRecord>[] result)
    {
        const SearchOption searchOption = SearchOption.AllDirectories;

        foreach (var filePath in Directory.EnumerateFiles(EnginePath.AssetBasePath, "*.*", searchOption))
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = fileInfo.Name;
            if(!FileUtils.TestFileName2(fileName, out AssetKind kind))
                continue;

            if (AssetManager.FileRegistry.HasFilePath(filePath)) continue;
            
            ExtractFileInfo(0, fileName, filePath, fileInfo, out var scanInfo);
            AssetManager.FileRegistry.RegisterFile(FileBinding.UnboundFile, in scanInfo);
        }
        
        foreach (var filePath in Directory.EnumerateFiles(EnginePath.AssetBasePath, "*.asset", searchOption))
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = fileInfo.Name;
            if(!FileUtils.TestFileName(fileName, out _, out _))
                continue;

            if (AssetManager.FileRegistry.HasFilePath(filePath)) continue;

            var record = AssetSerializer.LoadRecord(filePath);
            result[record.Kind.ToIndex()].Enqueue(record);
                
            // Asset file
            ExtractFileInfo(0, record.Name, filePath, fileInfo, out var scanInfo);
            var assetId = assetManager.RegisterScannedAsset(record, in scanInfo);
                
            // Dependent files
            RegisterBindings(assetId, record, filePath, Path.GetDirectoryName(filePath.AsSpan()));
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
*/
}