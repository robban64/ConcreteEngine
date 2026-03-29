using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Configuration.IO;
using static ConcreteEngine.Engine.Assets.Utils.AssetKindUtils;

namespace ConcreteEngine.Engine.Assets.IO;

internal static class AssetScanner
{
    public static void ScanAll(AssetStore store, Queue<AssetRecord>[] result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Length, AssetTypeCount);

        var shaderQueue = result[AssetKind.Shader.ToIndex()] = new Queue<AssetRecord>(16);
        var textureQueue = result[AssetKind.Texture.ToIndex()] = new Queue<AssetRecord>(64);
        var modelQueue = result[AssetKind.Model.ToIndex()] = new Queue<AssetRecord>(64);
        var materialQueue = result[AssetKind.Material.ToIndex()] = new Queue<AssetRecord>(64);

        ScanFiles(store, AssetKind.Shader, EnginePath.ShaderPath, FileUtils.ValidShaderExt, shaderQueue);
        ScanFiles(store, AssetKind.Texture, EnginePath.TexturePath, FileUtils.ValidTextureExt, textureQueue);
        ScanFiles(store, AssetKind.Model, EnginePath.ModelPath, FileUtils.ValidModelExt, modelQueue);
        ScanFiles(store, AssetKind.Material, EnginePath.MaterialPath, ReadOnlySpan<string>.Empty, materialQueue);
    }


    private static void ScanFiles(
        AssetStore store,
        AssetKind kind,
        string directory,
        ReadOnlySpan<string> validExt,
        Queue<AssetRecord> result)
    {
        var fileRegistry = store.FileRegistry;
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

            var info = new FileScanInfo(0, kind, AssetStorageKind.FileSystem);
            ExtractFileInfo(record.Name, fileInfo, ref info);
            
            var relativePath = Path.GetRelativePath(directory, filePath);
            relativePath = Path.Combine(relativeDirectory, relativePath);
            var assetId = store.RegisterScannedAsset(record, relativePath, in info);

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
            
            if(fileRegistry.HasFilePath(relativePath)) continue;
            
            var filename = Path.GetFileNameWithoutExtension(filePath);

            var info = new FileScanInfo(0, kind, AssetStorageKind.FileSystem);
            ExtractFileInfo(filename, fileInfo, ref info);

            store.RegisterUnboundFile(filename, relativePath, in info);
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
    
    
/*
    private static void ScanDirectory(
        AssetStore store,
        AssetKind kind,
        string directory,
        ReadOnlySpan<string> validExt,
        Queue<AssetRecord> result)
    {
        var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            var ext = Path.GetExtension(filePath.AsSpan());
            if (ext is ".asset")
            {
                var record = AssetSerializer.LoadRecord(filePath);
                RegisterAsset(store, record);
                result.Enqueue(record);
                continue;
            }

            if (!validExt.ContainsCharSpan(ext, StringComparison.OrdinalIgnoreCase)) continue;

            var filename = Path.GetFileNameWithoutExtension(filePath);
            if (filename.StartsWith('.')) continue;

            var relativePath = Path.GetRelativePath(directory, filePath);
            var assetPath = $"{filePath}.asset";
            if (File.Exists(assetPath)) continue;

            var info = new FileScanInfo(0) { StorageKind = AssetStorageKind.FileSystem };

            if (!ExtractFileInfo(filename, filePath, ref info)) return;

            store.RegisterFile(filename, relativePath, in info);

            try
            {
                AssetRecord? record = kind switch
                {
                    AssetKind.Texture => TextureRecord.Create(filename, relativePath),
                    AssetKind.Model => ModelRecord.Create(filename, relativePath),
                    AssetKind.Shader => null,
                    _ => throw new NotImplementedException($"Factory missing for {kind}")
                };

               // if (record is not null)
                    //AssetSerializer.WriteRecord(assetPath, record);
            }
            catch (NotSupportedException)
            {
                continue;
            }

        }
    }

    private static void RegisterAsset(AssetStore store, AssetRecord record)
    {
        switch (record)
        {
            case ModelRecord model: RegisterModel(store, model); break;
            case ShaderRecord shader: RegisterShader(store, shader); break;
            case TextureRecord texture: RegisterTexture(store, texture); break;
            case MaterialRecord: store.RegisterScannedAsset(record.GId, 0); break;
        }
    }

    private static void RegisterShader(AssetStore store, ShaderRecord record)
    {
        var (vsFile, fsFile) = ShaderRecord.GetFilenames(record);
        var vs = Path.Combine(EnginePath.ShaderCorePath, vsFile);
        var fs = Path.Combine(EnginePath.ShaderCorePath, fsFile);

        var vsInfo = new FileScanInfo(0) { Kind = AssetKind.Shader, StorageKind = AssetStorageKind.FileSystem };
        var fsInfo = new FileScanInfo(1) { Kind = AssetKind.Shader, StorageKind = AssetStorageKind.FileSystem };

        if (!ExtractFileInfo(record.Name, vs, ref vsInfo)) return;
        if (!ExtractFileInfo(record.Name, fs, ref fsInfo)) return;

        var assetId = store.RegisterScannedAsset(record.GId, 2);
        store.RegisterAssetBinding(assetId, record.Name, vsFile, in vsInfo);
        store.RegisterAssetBinding(assetId, record.Name, fsFile, in fsInfo);
    }

    private static void RegisterTexture(AssetStore store, TextureRecord record)
    {
        var assetId = store.RegisterScannedAsset(record.GId, record.Files.Count);

        byte idx = 0;
        foreach (var (_, filename) in record.Files)
        {
            var scanInfo =
                new FileScanInfo(idx++) { Kind = AssetKind.Texture, StorageKind = AssetStorageKind.FileSystem };
            var path = Path.Combine(EnginePath.TexturePath, filename);
            if (!ExtractFileInfo(record.Name, path, ref scanInfo))
            {
                Logger.LogString(LogScope.Engine, $"[Scanner] Skipped invalid texture: {record.Name}",
                    LogLevel.Critical);
                return;
            }

            store.RegisterAssetBinding(assetId, record.Name, filename, in scanInfo);
        }
    }

    private static void RegisterModel(AssetStore store, ModelRecord record)
    {
        var scanInfo = new FileScanInfo { Kind = AssetKind.Model, StorageKind = AssetStorageKind.FileSystem };
        var filename = AssetRecord.GetDefaultFilename(record);
        var path = Path.Combine(EnginePath.ModelPath, filename);

        if (!ExtractFileInfo(record.Name, path, ref scanInfo))
        {
            Logger.LogString(LogScope.Engine, $"[Scanner] Skipped invalid model: {record.Name}",
                LogLevel.Critical);
            return;
        }

        var assetId = store.RegisterScannedAsset(record.GId, 1);
        store.RegisterAssetBinding(assetId, record.Name, filename, in scanInfo);
    }
*/
}