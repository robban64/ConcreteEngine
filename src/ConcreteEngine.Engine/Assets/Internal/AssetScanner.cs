using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Utils;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Editor.Diagnostics;

namespace ConcreteEngine.Engine.Assets.Internal;

internal sealed class AssetScanner
{
    public Queue<AssetRecord>[] ScanEnqueueDirectory(AssetStore store, string rootPath)
    {
        var shaders = Directory.EnumerateFiles(EnginePath.ShaderPath, "*.asset", SearchOption.AllDirectories).Count();
        var textures = Directory.EnumerateFiles(EnginePath.TexturePath, "*.asset", SearchOption.AllDirectories).Count();
        var models = Directory.EnumerateFiles(EnginePath.ModelPath, "*.asset", SearchOption.AllDirectories).Count();
        var materials = Directory.EnumerateFiles(EnginePath.MaterialPath, "*.asset", SearchOption.AllDirectories)
            .Count();

        int count = shaders + textures + models + materials;
        store.EnsureStoreCapacity(count, shaders, textures, models, materials);

        var result = new Queue<AssetRecord>[AssetKindUtils.AssetTypeCount];
        result[(int)AssetKind.Shader - 1] = new Queue<AssetRecord>(shaders);
        result[(int)AssetKind.Texture - 1] = new Queue<AssetRecord>(textures);
        result[(int)AssetKind.Model - 1] = new Queue<AssetRecord>(models);
        result[(int)AssetKind.Material - 1] = new Queue<AssetRecord>(materials);

        var files = Directory.EnumerateFiles(rootPath, "*.asset", SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);

            var record = AssetSerializer.LoadRecord(filePath);
            ScanAsset(store, record);
            result[AssetKindUtils.ToAssetIndex(record.Kind)].Enqueue(record);
        }

        return result;
    }

    /*
    public void ScanDirectory(string rootPath)
    {
        var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            if (filePath.EndsWith(".asset") || filePath.EndsWith(".json") || filePath.EndsWith(".glsl")) continue;
            if (Path.GetFileName(filePath).StartsWith('.')) continue;

            var assetPath = $"{filePath}.asset";
            AssetRecord record;

            if (File.Exists(assetPath))
            {
                record = AssetManifestProvider.LoadRecord(assetPath);
            }
            else
            {
                var ext = Path.GetExtension(filePath);
                if (IsComplexAsset(ext)) continue;

                try
                {
                    record = CreateDefaultDescriptor(ext);
                    AssetManifestProvider.WriteRecord(assetPath, record);
                }
                catch (NotSupportedException)
                {
                    continue;
                }
            }

            ScanAsset(record);
        }
    }
    */
/*
    public void StartScanning(AssetManifestProvider mp)
    {
        var totalCount = 0;
        foreach (var manifest in mp.ManifestCatalog)
            totalCount += record.Count;

        _store.EnsureStoreCapacity(totalCount, mp.Shaderrecord.Count, mp.Texturerecord.Count,
            mp.Modelrecord.Count, mp.Materialrecord.Count);

        ScanTextures(mp.TextureManifest, EnginePath.TexturePath);
        ScanModels(mp.ModelManifest, EnginePath.MeshPath);
    }
*/
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

    private static AssetRecord CreateDefaultDescriptor(string ext)
    {
        if (!TryGetAssetKind(ext, out var kind))
            throw new NotSupportedException($"No default asset type for extension '{ext}'");

        return kind switch
        {
            AssetKind.Texture => TextureRecord.Create(EnginePath.TexturePath),
            AssetKind.Model => ModelRecord.Create(EnginePath.ModelPath),
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