using System.IO.Enumeration;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;

namespace ConcreteEngine.Engine.Assets;

internal sealed class AssetScanner(AssetManager assetManager)
{
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

    public void RunFullScan(AssetLoaderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        //RunMigration();

        avg1.BeginSample();
        ScanFiles();
        ScanAssets(ctx);
        avg1.EndSample();
        avg1.ResetAndPrint("Scanner took");
    }

    private void ScanFiles()
    {
        var fileRegistry = assetManager.Files;
        foreach (var scanInfo in MakeFileEnumerable())
        {
            fileRegistry.RegisterFile(true, in scanInfo);
        }
    }

    private void ScanAssets(AssetLoaderContext ctx)
    {
        foreach (var scanInfo in MakeAssetEnumerable())
        {
            var record = AssetSerializer.LoadRecord(scanInfo.RelativePath);
            ctx.Enqueue(record);

            var assetId = assetManager.RegisterScannedAsset(record, in scanInfo);

            var fileIndex = 1;
            var path = Path.GetDirectoryName(scanInfo.RelativePath.AsSpan());
            for (int i = 0; i < record.FileCount; i++)
            {
                var filePath = Path.Join(path, record.GetFile(i));
                assetManager.RegisterAssetBinding(fileIndex++, assetId, record.Kind, filePath);
            }
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
        if (!root.StartsWith(EnginePath.Root)) Throwers.InvalidArgument(nameof(root), root);
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