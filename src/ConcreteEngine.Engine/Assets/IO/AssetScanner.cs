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
            relativePath = Path.Combine(relativeDirectory, relativePath);
            var assetId = store.RegisterScannedAsset(record, relativePath, in info);

            // Dependent files
            var fileIndex = 1;
            foreach (var (_, localPath) in record.Files)
            {
                var bindingFullPath = Path.Combine(directory, localPath);
                var bindingPath = Path.Combine(relativeDirectory, localPath);

                info = new FileScanInfo((byte)fileIndex++, kind, AssetStorageKind.FileSystem);
                ExtractFileInfo(record.Name, new FileInfo(bindingFullPath), ref info);
                store.RegisterAssetBinding(assetId, record.Name, bindingPath, in info);
            }
        }

        // register unimported files
        foreach (var fileInfo in files)
        {
            var filePath = fileInfo.FullName;
            var fileSpan = fileInfo.Name.AsSpan();
            if (fileSpan.EndsWith(".asset") || fileSpan.StartsWith('.')) continue;

            var extIndex = fileSpan.LastIndexOf('.');
            if (extIndex < 0) continue;

            var ext = fileSpan.Slice(extIndex);
            if (!validExt.ContainsCharSpan(ext, StringComparison.OrdinalIgnoreCase))
                continue;

            var relativePath = Path.GetRelativePath(directory, filePath);
            relativePath = Path.Combine(relativeDirectory, relativePath);

            if (fileRegistry.HasFilePath(relativePath)) continue;

            var filename = Path.GetFileNameWithoutExtension(filePath);

            var info = new FileScanInfo(0, kind, AssetStorageKind.FileSystem);
            ExtractFileInfo(filename, fileInfo, ref info);

            fileRegistry.AddUnbound(filename, relativePath, in info);
        }
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


    private static bool IsFileHeaderValid(string path, byte[] magic)
    {
        try
        {
            using var fs = File.OpenRead(path);
            if (fs.Length < magic.Length) return false;

            Span<byte> buffer = stackalloc byte[magic.Length];
            int read = fs.Read(buffer);

            return read == magic.Length && buffer.SequenceEqual(magic);
        }
        catch
        {
            return false;
        }
    }

    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] JpgHeader = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] FbxHeader = [0x4B, 0x61, 0x79, 0x64];
    private static readonly byte[] GltfHeader = [0x67, 0x6C, 0x54, 0x46];

    private static byte[]? GetMagicBytesForPath(string path)
    {
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return PngHeader;
        if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)) return JpgHeader;
        if (path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) return JpgHeader;
        if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) return FbxHeader;
        if (path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase)) return GltfHeader;
        return null;
    }

}