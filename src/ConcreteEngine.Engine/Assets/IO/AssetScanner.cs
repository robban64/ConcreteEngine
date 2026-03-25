using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Editor.Diagnostics;
using static ConcreteEngine.Engine.Assets.Utils.AssetKindUtils;

namespace ConcreteEngine.Engine.Assets.IO;

internal readonly struct ScanAssetCount(int shaderCount, int modelCount, int textureCount, int materialCount)
{
    public readonly int ShaderCount = shaderCount;
    public readonly int ModelCount = modelCount;
    public readonly int TextureCount = textureCount;
    public readonly int MaterialCount = materialCount;
    public int TotalCount => ShaderCount + ModelCount + TextureCount + MaterialCount;
}

internal static class AssetScanner
{
    public static ScanAssetCount ScanAssetCount()
    {
        const SearchOption flag = SearchOption.AllDirectories;
        var shaders = Directory.EnumerateFiles(EnginePath.ShaderPath, "*.asset", flag).Count();
        var models = Directory.EnumerateFiles(EnginePath.ModelPath, "*.asset", flag).Count();
        var textures = Directory.EnumerateFiles(EnginePath.TexturePath, "*.asset", flag).Count();
        var materials = Directory.EnumerateFiles(EnginePath.MaterialPath, "*.asset", flag).Count();
        return new ScanAssetCount(shaders, models, textures, materials);
    }

    public static Queue<AssetRecord>[] ScanAll(in ScanAssetCount assetCount, AssetStore store)
    {
        var result = new Queue<AssetRecord>[AssetTypeCount];
        result[ToIndex(AssetKind.Shader)] = new Queue<AssetRecord>(assetCount.ShaderCount);
        result[ToIndex(AssetKind.Texture)] = new Queue<AssetRecord>(assetCount.TextureCount);
        result[ToIndex(AssetKind.Model)] = new Queue<AssetRecord>(assetCount.ModelCount);
        result[ToIndex(AssetKind.Material)] = new Queue<AssetRecord>(assetCount.MaterialCount);

        ScanDirectory(store, AssetKind.Shader, EnginePath.ShaderPath, FileUtils.ValidShaderExt,
            result[ToIndex(AssetKind.Shader)]);
        
        ScanDirectory(store, AssetKind.Texture, EnginePath.TexturePath, FileUtils.ValidTextureExt,
            result[ToIndex(AssetKind.Texture)]);
        
        ScanDirectory(store, AssetKind.Model, EnginePath.ModelPath, FileUtils.ValidModelExt,
            result[ToIndex(AssetKind.Model)]);
        
        ScanDirectory(store, AssetKind.Material, EnginePath.MaterialPath, ReadOnlySpan<string>.Empty,
            result[ToIndex(AssetKind.Material)]);
        
        return result;
    }

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
            var ext = Path.GetExtension(filePath);
            if (ext.Equals(".asset"))
            {
                var record = AssetSerializer.LoadRecord(filePath);
                RegisterAsset(store, record);
                result.Enqueue(record);
                continue;
            }

            if (!validExt.Contains(ext, StringComparer.OrdinalIgnoreCase)) continue;

            var filename = Path.GetFileName(filePath);
            if (filename.StartsWith('.')) continue;

            var relativePath = Path.GetRelativePath(directory, filePath);
            var assetPath = $"{filePath}.asset";
            if (File.Exists(assetPath)) continue;

            try
            {
                AssetRecord? record = kind switch
                {
                    AssetKind.Texture => TextureRecord.Create(filename, relativePath),
                    AssetKind.Model => ModelRecord.Create(filename, relativePath),
                    AssetKind.Shader => null,
                    _ => throw new NotImplementedException($"Factory missing for {kind}")
                };

                if (record is not null)
                    AssetSerializer.WriteRecord(assetPath, record);
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
            case ModelRecord model: RegisterModel(store, model, EnginePath.ModelPath); break;
            case ShaderRecord shader: RegisterShader(store, shader, EnginePath.ShaderCorePath); break;
            case TextureRecord texture: RegisterTexture(store, texture, EnginePath.TexturePath); break;
            case MaterialRecord: store.RegisterScannedAsset(record.GId, 0); break;
        }
    }

    private static void RegisterShader(AssetStore store, ShaderRecord record, string rootPath)
    {
        var (vsFile, fsFile) = ShaderRecord.GetFilenames(record);
        var vs = Path.Combine(rootPath, vsFile);
        var fs = Path.Combine(rootPath, fsFile);

        var vsInfo = new FileScanInfo
        {
            FileIndex = 0, Kind = AssetKind.Shader, StorageKind = AssetStorageKind.FileSystem
        };
        var fsInfo = new FileScanInfo
        {
            FileIndex = 1, Kind = AssetKind.Shader, StorageKind = AssetStorageKind.FileSystem
        };

        if (!ExtractFileInfo(record.Name, vs, ref vsInfo)) return;
        if (!ExtractFileInfo(record.Name, fs, ref fsInfo)) return;

        var assetId = store.RegisterScannedAsset(record.GId, 2);
        store.RegisterScannedSpec(assetId, record.Name, vsFile, in vsInfo);
        store.RegisterScannedSpec(assetId, record.Name, fsFile, in fsInfo);
    }

    private static void RegisterTexture(AssetStore store, TextureRecord record, string rootPath)
    {
        var scanInfo = new FileScanInfo { Kind = AssetKind.Texture, StorageKind = AssetStorageKind.FileSystem };
        var filename = AssetRecord.GetDefaultFilename(record);
        var fullPath = Path.Combine(rootPath, filename);

        if (!ExtractFileInfo(record.Name, fullPath, ref scanInfo))
        {
            Logger.LogString(LogScope.Engine, $"[Scanner] Skipped invalid texture: {record.Name}",
                LogLevel.Critical);
            return;
        }

        var assetId = store.RegisterScannedAsset(record.GId, 1);
        store.RegisterScannedSpec(assetId, record.Name, filename, in scanInfo);
    }

    private static void RegisterModel(AssetStore store, ModelRecord record, string rootPath)
    {
        var scanInfo = new FileScanInfo { Kind = AssetKind.Model, StorageKind = AssetStorageKind.FileSystem };
        var filename = AssetRecord.GetDefaultFilename(record);
        var fullPath = Path.Combine(rootPath, filename);

        if (!ExtractFileInfo(record.Name, fullPath, ref scanInfo))
        {
            Logger.LogString(LogScope.Engine, $"[Scanner] Skipped invalid model: {record.Name}",
                LogLevel.Critical);
            return;
        }

        var assetId = store.RegisterScannedAsset(record.GId, 1);
        store.RegisterScannedSpec(assetId, record.Name, filename, in scanInfo);
    }


    private static bool ExtractFileInfo(string logicalName, string fullPath, scoped ref FileScanInfo scanInfo)
    {
        var info = new FileInfo(fullPath);
        if (!info.Exists) return false;

        //var expectedMagic = GetMagicBytesForPath(fullPath);
        //if (expectedMagic != null && !IsFileHeaderValid(fullPath, expectedMagic))
        //    return false;

        scanInfo.Source = logicalName;
        scanInfo.LastWriteTime = info.LastWriteTime;
        scanInfo.SizeBytes = info.Length;
        scanInfo.IsValid = true;
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