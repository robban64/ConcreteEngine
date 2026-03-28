namespace ConcreteEngine.Engine.Assets.IO;
/*
internal abstract class AssetTypeScanner
{
    public abstract string DirectoryName { get; }
    public abstract AssetKind Kind { get; }
    public abstract ReadOnlySpan<string> ValidExtensions { get; }
}

internal abstract class AssetTypeScanner<TRecord> : AssetTypeScanner where TRecord : AssetRecord
{
    protected AssetTypeScanner()
    {
    }
    
    public void ScanAll()
    {
        var files = Directory.EnumerateFiles(DirectoryName, "*.*", SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            var ext = Path.GetExtension(filePath);
            if (ext.Equals(".asset") || !ValidExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) 
                continue;

            var filename = Path.GetFileName(filePath);
            if (filename.StartsWith('.')) continue;

            var relativePath = Path.GetRelativePath(DirectoryName, filePath);
            var assetPath = $"{filePath}.asset";
            if (File.Exists(assetPath)) continue;

            try
            {
                var record = CreateDefaultDescriptor(filename, relativePath, ext);
                AssetSerializer.WriteRecord(assetPath, record);
            }
            catch (NotSupportedException)
            {
                continue;
            }
        }
    }

    private static void ScanModel(AssetStore store, ModelRecord record, string rootPath)
    {
        var scanInfo = new FileScanInfo { Kind = AssetKind.Model, StorageKind = AssetStorageKind.FileSystem };
        var filename = AssetRecord.GetDefaultFilename(record);

        var fullPath = Path.Combine(rootPath, filename);

        if (!AssetScanner.TryValidateFileInfo(record.Name, fullPath, ref scanInfo))
        {
            Logger.LogString(LogScope.Engine, $"[Scanner] Skipped invalid model: {record.Name}",
                LogLevel.Critical);
            return;
        }

        var assetId = store.RegisterScannedAsset(record.GId, 1);
        store.RegisterScannedSpec(assetId, record.Name, filename, in scanInfo);
    }


    public void LoadRecords(AssetStore store, Queue<TRecord> result)
    {
        var files = Directory.EnumerateFiles(DirectoryName, "*.asset", SearchOption.AllDirectories);
        foreach (var filePath in files)
        {
            var record = AssetSerializer.LoadRecord<TRecord>(filePath);
            RegisterRecord(store, record);
            result.Enqueue(record);
        }
    }

    protected abstract void RegisterRecord(AssetStore store, TRecord record);
}*/