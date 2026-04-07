using System.Security.Cryptography;

// ReSharper disable UseUtf8StringLiteral

namespace ConcreteEngine.Core.Engine.Configuration;

public static class FileUtils
{
    public static readonly string[] ValidTextureExt = [".png", ".jpg", ".jpeg", ".tga", ".bmp"];
    public static readonly string[] ValidModelExt = [".glb", ".gltf", ".fbx", ".obj"];
    public static readonly string[] ValidShaderExt = [".glsl"];

    public static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47];
    public static readonly byte[] JpgHeader = [0xFF, 0xD8, 0xFF];
    public static readonly byte[] FbxHeader = [0x4B, 0x61, 0x79, 0x64];
    public static readonly byte[] GltfHeader = [0x67, 0x6C, 0x54, 0x46];

    public static string ComputeSha256Hex(string path)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(path);
        var hash = sha.ComputeHash(fs);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }


    public static bool IsFileHeaderValid(string path, ReadOnlySpan<byte> magic)
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

    public static ReadOnlySpan<byte> GetMagicBytesForPath(ReadOnlySpan<char> path)
    {
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return PngHeader;
        if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)) return JpgHeader;
        if (path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) return JpgHeader;
        if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) return FbxHeader;
        if (path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase)) return GltfHeader;
        return default;
    }
}