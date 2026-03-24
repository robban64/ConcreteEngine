using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Editor.Diagnostics;
using static ConcreteEngine.Engine.Assets.Utils.AssetKindUtils;

namespace ConcreteEngine.Engine.Assets.Internal;

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

    public static Queue<AssetRecord>[] ScanEnqueueDirectory(in ScanAssetCount assetCount, AssetStore store)
    {
        var result = new Queue<AssetRecord>[AssetTypeCount];
        result[ToAssetIndex(AssetKind.Shader)] = new Queue<AssetRecord>(assetCount.ShaderCount);
        result[ToAssetIndex(AssetKind.Texture)] = new Queue<AssetRecord>(assetCount.TextureCount);
        result[ToAssetIndex(AssetKind.Model)] = new Queue<AssetRecord>(assetCount.ModelCount);
        result[ToAssetIndex(AssetKind.Material)] = new Queue<AssetRecord>(assetCount.MaterialCount);

        EnqueueDirectory(store, EnginePath.ShaderPath, result[ToAssetIndex(AssetKind.Shader)]);
        EnqueueDirectory(store, EnginePath.TexturePath, result[ToAssetIndex(AssetKind.Texture)]);
        EnqueueDirectory(store, EnginePath.ModelPath, result[ToAssetIndex(AssetKind.Model)]);
        EnqueueDirectory(store, EnginePath.MaterialPath, result[ToAssetIndex(AssetKind.Material)]);

        return result;
        
        static void EnqueueDirectory(AssetStore store, string path, Queue<AssetRecord> result)
        {
            var files = Directory.EnumerateFiles(path, "*.asset", SearchOption.AllDirectories);

            foreach (var filePath in files)
            {
                if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);

                var record = AssetSerializer.LoadRecord(filePath);
                ScanAsset(store, record);
                result.Enqueue(record);
            }
        }

    }

    public static void ScanDirectory()
    {
        var files = Directory.EnumerateFiles(EnginePath.ModelPath, "*.*", SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            if (filePath.EndsWith(".asset") || filePath.EndsWith(".json") || filePath.EndsWith(".glsl")) continue;

            var filename = Path.GetFileName(filePath);
            if (filename.StartsWith('.')) continue;

            var relativePath = Path.GetRelativePath(EnginePath.ModelPath, filePath);
            var assetPath = $"{filePath}.asset";
            if (File.Exists(assetPath)) continue;

            var ext = Path.GetExtension(relativePath);
            if (IsComplexAsset(ext)) continue;

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

    private static void ScanAsset(AssetStore store, AssetRecord record)
    {
        switch (record)
        {
            case ModelRecord model: ScanModel(store, model, EnginePath.ModelPath); break;
            case ShaderRecord shader: ScanShader(store, shader, EnginePath.ShaderCorePath); break;
            case TextureRecord texture: ScanTexture(store, texture, EnginePath.TexturePath); break;
            case MaterialRecord: store.RegisterScannedAsset(record.GId, 0); break;
        }
    }

    private static void ScanShader(AssetStore store, ShaderRecord record, string rootPath)
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

        if (!TryValidateFileInfo(record.Name, vs, ref vsInfo)) return;
        if (!TryValidateFileInfo(record.Name, fs, ref fsInfo)) return;

        var assetId = store.RegisterScannedAsset(record.GId, 2);
        store.RegisterScannedSpec(assetId, record.Name, vsFile, in vsInfo);
        store.RegisterScannedSpec(assetId, record.Name, fsFile, in fsInfo);
    }

    private static void ScanTexture(AssetStore store, TextureRecord record, string rootPath)
    {
        var scanInfo = new FileScanInfo { Kind = AssetKind.Texture, StorageKind = AssetStorageKind.FileSystem };
        var filename = AssetRecord.GetDefaultFilename(record);
        var fullPath = Path.Combine(rootPath, filename);

        if (!TryValidateFileInfo(record.Name, fullPath, ref scanInfo))
        {
            Logger.LogString(LogScope.Engine, $"[Scanner] Skipped invalid texture: {record.Name}",
                LogLevel.Critical);
            return;
        }

        var assetId = store.RegisterScannedAsset(record.GId, 1);
        store.RegisterScannedSpec(assetId, record.Name, filename, in scanInfo);
    }

    private static void ScanModel(AssetStore store, ModelRecord record, string rootPath)
    {
        var scanInfo = new FileScanInfo { Kind = AssetKind.Model, StorageKind = AssetStorageKind.FileSystem };
        var filename = AssetRecord.GetDefaultFilename(record);

        var fullPath = Path.Combine(rootPath, filename);

        if (!TryValidateFileInfo(record.Name, fullPath, ref scanInfo))
        {
            Logger.LogString(LogScope.Engine, $"[Scanner] Skipped invalid model: {record.Name}",
                LogLevel.Critical);
            return;
        }

        var assetId = store.RegisterScannedAsset(record.GId, 1);
        store.RegisterScannedSpec(assetId, record.Name, filename, in scanInfo);
    }

    private static AssetRecord CreateDefaultDescriptor(string filename, string relativePath, string ext)
    {
        if (!TryGetAssetKind(ext, out var kind))
            throw new NotSupportedException($"No default asset type for extension '{ext}'");

        return kind switch
        {
            AssetKind.Texture => TextureRecord.Create(filename,relativePath),
            AssetKind.Model => ModelRecord.Create(filename,relativePath),
            AssetKind.Shader => throw new InvalidOperationException(
                "Shaders cannot be auto-discovered. Create the .asset file manually."),
            _ => throw new NotImplementedException($"Factory missing for {kind}")
        };
    }

    private static bool IsComplexAsset(string ext) => ext is ".vert" or ".frag" or ".glsl";

    private static bool TryGetAssetKind(string ext, out AssetKind kind)
    {
        kind = ext.ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".tga" or ".bmp" => AssetKind.Texture,
            ".fbx" or ".obj" or ".gltf" or ".glb" => AssetKind.Model,
            _ => AssetKind.Unknown
        };

        return kind != AssetKind.Unknown;
    }


    private static bool TryValidateFileInfo(string logicalName, string fullPath, ref FileScanInfo scanInfo)
    {
        var info = new FileInfo(fullPath);
        if (!info.Exists) return false;

        byte[]? expectedMagic = GetMagicBytesForPath(fullPath);
        if (expectedMagic != null)
        {
            if (!IsFileHeaderValid(fullPath, expectedMagic))
            {
                return false;
            }
        }

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