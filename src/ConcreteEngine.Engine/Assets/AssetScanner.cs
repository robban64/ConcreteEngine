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
        ScanFiles(AssetKind.Shader, EnginePath.ShaderPath, FileUtils.ValidShaderExt, shaderQueue);
        ScanFiles(AssetKind.Texture, EnginePath.TexturePath, FileUtils.ValidTextureExt, textureQueue);
        ScanFiles(AssetKind.Model, EnginePath.ModelPath, FileUtils.ValidModelExt, modelQueue);
        ScanFiles(AssetKind.Material, EnginePath.MaterialPath, default, materialQueue);
        avg1.EndSample();
        avg1.ResetAndPrint("Scanner took");
    }

    private AvgFrameTimer avg1;

    private void ScanFiles(
        AssetKind kind,
        string directory,
        ReadOnlySpan<string> validExt,
        Queue<AssetRecord> result)
    {
        var assets = assetManager;

        var di = new DirectoryInfo(directory);
        var files = di.GetFiles("*.*", SearchOption.AllDirectories);
        var relativeDirectory = directory.Substring(directory.LastIndexOf('/') + 1);

        // register assets and related files
        foreach (var fileInfo in files)
        {
            if (!fileInfo.Name.EndsWith(".asset")) continue;

            var filePath = fileInfo.FullName;
            var record = AssetSerializer.LoadRecord(filePath);
            result.Enqueue(record);

            // Root file
            var info = new FileScanInfo(0, kind, AssetStorage.FileSystem);
            ExtractFileInfo(record.Name, fileInfo, ref info);

            var relativePath = Path.GetRelativePath(directory, filePath);
            relativePath = Path.Join(relativeDirectory, relativePath);
            var assetId = assets.RegisterScannedAsset(record, relativePath, in info);

            // Dependent files
            RegisterBindings(assets, assetId, record, directory, relativeDirectory);
        }

        // register unimported files
        foreach (var fileInfo in files)
        {
            RegisterUnimportedFile(assets.Files, fileInfo, kind, directory, relativeDirectory, validExt);
        }

    }

    private static void RegisterBindings(AssetManager assets, AssetId assetId, AssetRecord record, string directory,
        string relativeDirectory)
    {
        var fileIndex = 1;
        var info = new FileScanInfo(0, record.Kind, AssetStorage.FileSystem);
        foreach (var (_, localPath) in record.Files)
        {
            var bindingFullPath = Path.Join(directory, localPath);
            var bindingPath = Path.Join(relativeDirectory, localPath);

            info = new FileScanInfo((byte)fileIndex++, record.Kind, AssetStorage.FileSystem);
            ExtractFileInfo(record.Name, new FileInfo(bindingFullPath), ref info);
            assets.RegisterAssetBinding(assetId, record.Name, bindingPath, in info);
        }
    }

    private static void RegisterUnimportedFile(AssetFileRegistry fileRegistry, FileInfo fileInfo, AssetKind kind,
        string directory, string relativeDirectory, ReadOnlySpan<string> validExt)
    {
        var filePath = fileInfo.FullName;
        var fileSpan = fileInfo.Name.AsSpan();
        if (fileSpan.EndsWith(".asset") || fileSpan.StartsWith('.')) return;

        var extIndex = fileSpan.LastIndexOf('.');
        if (extIndex < 0) return;

        var ext = fileSpan.Slice(extIndex);
        if (!validExt.ContainsCharSpan(ext, StringComparison.OrdinalIgnoreCase))
            return;

        var relativePath = Path.GetRelativePath(directory, filePath);
        relativePath = Path.Join(relativeDirectory, relativePath);

        if (fileRegistry.HasFilePath(relativePath)) return;

        var filename = Path.GetFileNameWithoutExtension(filePath);

        var info = new FileScanInfo(0, kind, AssetStorage.FileSystem);
        ExtractFileInfo(filename, fileInfo, ref info);

        fileRegistry.RegisterUnbound(filename, relativePath, in info);
    }


    private static bool ExtractFileInfo(string name, FileInfo info, scoped ref FileScanInfo scanInfo)
    {
        if (!info.Exists) return false;

        //var expectedMagic = GetMagicBytesForPath(fullPath);
        //if (expectedMagic != null && !IsFileHeaderValid(fullPath, expectedMagic))
        //    return false;

        scanInfo.Source = name;
        scanInfo.LastWriteTime = info.LastWriteTime;
        scanInfo.SizeBytes = info.Length;
        scanInfo.IsValid = true;
        scanInfo.Storage = AssetStorage.FileSystem;
        return true;
    }
    
    public static void ScanExisting(string path, AssetFile file, ref FileScanInfo info)
    {
        if (!File.Exists(path)) throw new FileNotFoundException(path);
        var fileInfo = new FileInfo(path);
        ExtractFileInfo(file.LogicalName, fileInfo, ref info);
    }

}