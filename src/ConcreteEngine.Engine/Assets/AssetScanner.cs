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

internal sealed class AssetScanner(AssetManager assetManager)
{
    public void RunFullScan(Queue<AssetRecord>[] result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Length, AssetTypeCount);

        RunMigration();
        
        var shaderQueue = result[AssetKind.Shader.ToIndex()] = new Queue<AssetRecord>(16);
        var textureQueue = result[AssetKind.Texture.ToIndex()] = new Queue<AssetRecord>(64);
        var modelQueue = result[AssetKind.Model.ToIndex()] = new Queue<AssetRecord>(64);
        var materialQueue = result[AssetKind.Material.ToIndex()] = new Queue<AssetRecord>(64);

        avg1.BeginSample();
        ScanAllFiles(result);
        avg1.EndSample();
        avg1.ResetAndPrint("Scanner took");
    }

    private AvgFrameTimer avg1;

    private void RunMigration()
    {
        foreach (var scanInfo in MakeAssetEnumerable())
        {
            var record = AssetSerializer.LoadRecord(scanInfo.RelativePath);
            AssetSerializer.WriteRecord(scanInfo.RelativePath, record);
        }

        throw new InvalidOperationException("Done");
    }

    private void ScanAllFiles(Queue<AssetRecord>[] result)
    {
        foreach (var scanInfo in MakeFileEnumerable())
        {
            AssetManager.FileRegistry.RegisterFile(FileBinding.UnboundFile, scanInfo.Name, in scanInfo);
        }

        foreach (var scanInfo in MakeAssetEnumerable())
        {
            var record = AssetSerializer.LoadRecord(scanInfo.RelativePath);
            var assetId = assetManager.RegisterScannedAsset(record, in scanInfo);

            var fileIndex = 1;
            var path = scanInfo.RelativePath.AsSpan(0, scanInfo.RelativePath.LastIndexOf('/'));
            if (record.Files is not null)
            {
                foreach (var localPath in record.Files.Values)
                {
                    var filePath = Path.Join(path, localPath);
                    assetManager.RegisterAssetBinding(fileIndex++, assetId, record.Kind, filePath);
                }
            }

            result[record.Kind.ToIndex()].Enqueue(record);
        }
    }

    private static FileSystemEnumerable<FileScanInfo> MakeFileEnumerable()
    {
        return new FileSystemEnumerable<FileScanInfo>(
            directory: EnginePath.AssetBasePath,
            options: new EnumerationOptions { RecurseSubdirectories = true, },
            transform: static (ref entry) =>
            {
                var fileName = entry.FileName.ToString();
                var path = entry.ToSpecifiedFullPath();
                return new FileScanInfo(fileName, path, entry.Length, entry.LastWriteTimeUtc.DateTime);
            }
        )
        {
            ShouldIncludePredicate = static (ref entry) =>
                !entry.IsDirectory && !entry.IsHidden && FileUtils.TestFileExtension(entry.FileName, out _)
        };
    }

    private static FileSystemEnumerable<FileScanInfo> MakeAssetEnumerable(string root = EnginePath.AssetBasePath)
    {
        if(!root.StartsWith(EnginePath.Root)) Throwers.InvalidArgument(nameof(root), root);
        return new FileSystemEnumerable<FileScanInfo>(
            directory: root,
            options: new EnumerationOptions() { RecurseSubdirectories = true, },
            transform: static (ref entry) =>
            {
                var filePath = entry.ToSpecifiedFullPath();
                return new FileScanInfo(string.Empty, filePath, entry.Length, entry.LastWriteTimeUtc.DateTime);
            }
        )
        {   
            ShouldIncludePredicate = static (ref entry) =>
                !entry.IsDirectory && !entry.IsHidden && Path.GetExtension(entry.FileName) is ".asset",
        };
    }

}