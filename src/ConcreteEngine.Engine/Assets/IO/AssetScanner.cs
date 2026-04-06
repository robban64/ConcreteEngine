using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Configuration.IO;
using static ConcreteEngine.Engine.Assets.Utils.AssetKindUtils;

namespace ConcreteEngine.Engine.Assets.IO;

internal static class AssetScanner
{
    public static void ScanAll(AssetStore store, AssetFileRegistry files, Queue<AssetRecord>[] result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Length, AssetTypeCount);

        var shaderQueue = result[AssetKind.Shader.ToIndex()] = new Queue<AssetRecord>(16);
        var textureQueue = result[AssetKind.Texture.ToIndex()] = new Queue<AssetRecord>(64);
        var modelQueue = result[AssetKind.Model.ToIndex()] = new Queue<AssetRecord>(64);
        var materialQueue = result[AssetKind.Material.ToIndex()] = new Queue<AssetRecord>(64);

        ScanFiles(store, files, AssetKind.Shader, EnginePath.ShaderPath, FileUtils.ValidShaderExt, shaderQueue);
        ScanFiles(store, files, AssetKind.Texture, EnginePath.TexturePath, FileUtils.ValidTextureExt, textureQueue);
        ScanFiles(store, files, AssetKind.Model, EnginePath.ModelPath, FileUtils.ValidModelExt, modelQueue);
        ScanFiles(store, files, AssetKind.Material, EnginePath.MaterialPath, default, materialQueue);
    }


    private static void ScanFiles(
        AssetStore store,
        AssetFileRegistry fileRegistry,
        AssetKind kind,
        string directory,
        ReadOnlySpan<string> validExt,
        Queue<AssetRecord> result)
    {
        var di = new DirectoryInfo(directory);
        var files = di.GetFiles("*.*", SearchOption.AllDirectories);
        var relativeDirectory = directory.Substring(directory.IndexOf('/') + 1);
        
        // register assets and related files
        foreach (var fileInfo in files)
        {
            if (!fileInfo.Name.EndsWith(".asset")) continue;

            var filePath = fileInfo.FullName;
            var record = AssetSerializer.LoadRecord(filePath);
            result.Enqueue(record);

            // Root file
            var info = new FileScanInfo(0, kind, AssetStorageKind.FileSystem);
            ExtractFileInfo(record.Name, fileInfo, ref info);

            var relativePath = Path.GetRelativePath(directory, filePath);
            relativePath = Path.Join(relativeDirectory, relativePath);
            var assetId = store.RegisterScannedAsset(record, relativePath, in info);

            // Dependent files
            RegisterBindings(store, assetId, record, directory, relativeDirectory);
        }

        // register unimported files
        foreach (var fileInfo in files)
        {
            RegisterUnimportedFile(fileRegistry, fileInfo, kind, directory, relativeDirectory, validExt);
        }
    }

    private static void RegisterBindings(AssetStore store, AssetId assetId, AssetRecord record, string directory, string relativeDirectory)
    {
        var fileIndex = 1;
        var info = new FileScanInfo(0, record.Kind, AssetStorageKind.FileSystem);
        foreach (var (_, localPath) in record.Files)
        {
            var bindingFullPath = Path.Join(directory, localPath);
            var bindingPath = Path.Join(relativeDirectory, localPath);

            info = new FileScanInfo((byte)fileIndex++, record.Kind, AssetStorageKind.FileSystem);
            ExtractFileInfo(record.Name, new FileInfo(bindingFullPath), ref info);
            store.RegisterAssetBinding(assetId, record.Name, bindingPath, in info);
        }
    }

    private static void RegisterUnimportedFile(AssetFileRegistry fileRegistry, FileInfo fileInfo, AssetKind kind, string directory, string relativeDirectory, ReadOnlySpan<string> validExt)
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

        var info = new FileScanInfo(0, kind, AssetStorageKind.FileSystem);
        ExtractFileInfo(filename, fileInfo, ref info);

        fileRegistry.AddUnbound(filename, relativePath, in info);
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
        scanInfo.StorageKind = AssetStorageKind.FileSystem;
        return true;
    }



}