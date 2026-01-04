using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Internal;

internal ref struct FileScanInfo
{
    public long SizeBytes;
    public DateTime LastWriteTime;
    public string? ContentHash;
    public string? Source;

    public AssetKind Kind;
    public AssetStorageKind StorageKind;
    public bool IsValid;
    public byte FileIndex;
}

internal sealed class AssetScanner
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] JpgHeader = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] FbxHeader = [0x4B, 0x61, 0x79, 0x64];
    private static readonly byte[] GltfHeader = [0x67, 0x6C, 0x54, 0x46];

    private readonly AssetStore _store;

    public AssetScanner(AssetStore store)
    {
        _store = store;
    }

    public void StartScanning(AssetManifestProvider manifestProvider)
    {
        var totalCount = 0;
        foreach (var manifest in manifestProvider.ManifestCatalog)
            totalCount += manifest.Count;

        _store.EnsureStoreCapacity(totalCount);

        ScanTextures(manifestProvider.TextureManifest, EnginePath.TexturePath);
        ScanModels(manifestProvider.ModelManifest, EnginePath.MeshPath);
    }

    private void ScanTextures(TextureManifest manifests, string rootPath)
    {
        foreach (var manifest in manifests.Records)
        {
            var scanInfo = new FileScanInfo { Kind = AssetKind.Texture2D, StorageKind = AssetStorageKind.FileSystem };

            var fullPath = Path.Combine(rootPath, manifest.Filename);

            if (!TryValidateFileInfo(manifest.Name, fullPath, ref scanInfo))
            {
                Logger.LogString(LogScope.Engine, $"[Scanner] Skipped invalid texture: {manifest.Name}",
                    LogLevel.Critical);
                continue;
            }

            var assetId = _store.RegisterScannedAsset(1);
            _store.RegisterScannedSpec(assetId, manifest.Name, manifest.Filename, in scanInfo);
        }
    }

    private void ScanModels(MeshManifest manifests, string rootPath)
    {
        foreach (var manifest in manifests.Records)
        {
            var scanInfo = new FileScanInfo { Kind = AssetKind.Model, StorageKind = AssetStorageKind.FileSystem };

            var fullPath = Path.Combine(rootPath, manifest.Filename);

            if (!TryValidateFileInfo(manifest.Name, fullPath, ref scanInfo))
            {
                continue;
            }

            var assetId = _store.RegisterScannedAsset(1);
            _store.RegisterScannedSpec(assetId, manifest.Name, manifest.Filename, in scanInfo);
        }
    }

    private void ScanShader(ShaderManifest manifests, string rootPath)
    {
        foreach (var manifest in manifests.Records)
        {
            var vs = Path.Combine(rootPath, manifest.VertexFilename);
            var fs = Path.Combine(rootPath, manifest.FragmentFilename);

            var vsInfo = new FileScanInfo { Kind = AssetKind.Shader, StorageKind = AssetStorageKind.FileSystem };
            var fsInfo = new FileScanInfo { Kind = AssetKind.Shader, StorageKind = AssetStorageKind.FileSystem };

            if (!TryValidateFileInfo(manifest.Name, vs, ref vsInfo)) continue;
            if (!TryValidateFileInfo(manifest.Name, fs, ref fsInfo)) continue;

            var assetId = _store.RegisterScannedAsset(2);
            _store.RegisterScannedSpec(assetId, manifest.Name, manifest.VertexFilename, in vsInfo);
            _store.RegisterScannedSpec(assetId, manifest.Name, manifest.FragmentFilename, in fsInfo);
        }
    }

    private static bool TryValidateFileInfo(string logicalName, string fullPath, ref FileScanInfo scanInfo)
    {
        var info = new FileInfo(fullPath);

        if (!info.Exists)
        {
            return false;
        }

        byte[]? expectedMagic = GetMagicBytesForPath(fullPath);
        if (expectedMagic != null)
        {
            if (!IsFileHeaderValid(fullPath, expectedMagic))
            {
                return false;
            }
        }

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