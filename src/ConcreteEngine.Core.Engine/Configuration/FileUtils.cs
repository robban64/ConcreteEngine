using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;

// ReSharper disable UseUtf8StringLiteral

namespace ConcreteEngine.Core.Engine.Configuration;

public static class FileUtils
{
    private const string ValidTextureExtension = ".png;.jpg;.jpeg;.tga;.bmp";
    private const string ValidModelExtension = ".glb;.gltf;.fbx;.obj";
    private const string ValidShaderExtension = ".glsl";
    private const string ValidMaterialExtension = ".mat";

    //public static readonly string[] ValidTextureExt = [".glb;.gltf;.fbx;.tga;.bmp", "", ".jpeg", ".tga", ".bmp"];
    //public static readonly string[] ValidModelExt = [".glb", ".gltf", ".fbx", ".obj"];
    //public static readonly string[] ValidShaderExt = [".glsl"];

    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] JpgHeader = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] FbxHeader = [0x4B, 0x61, 0x79, 0x64];
    private static readonly byte[] GltfHeader = [0x67, 0x6C, 0x54, 0x46];

    public static string GetValidExtensions(AssetKind kind) => kind switch
    {
        AssetKind.Shader => ValidShaderExtension,
        AssetKind.Model => ValidModelExtension,
        AssetKind.Texture => ValidTextureExtension,
        AssetKind.Material => ValidMaterialExtension,
        _ => Throwers.Unreachable<string>(nameof(kind))
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TestFileName(ReadOnlySpan<char> fileName, out AssetKind kind, out bool isAssetFile)
    {
        kind = AssetKind.Unknown;

        var ext = Path.GetExtension(fileName);
        if (fileName.StartsWith('.') || ext.IsEmpty) return isAssetFile = false;

        isAssetFile = ext is ".asset";
        if (isAssetFile) return true;

        if (ValidShaderExtension.Contains(ext, StringComparison.OrdinalIgnoreCase)) kind = AssetKind.Shader;
        else if (ValidModelExtension.Contains(ext, StringComparison.OrdinalIgnoreCase)) kind = AssetKind.Model;
        else if (ValidTextureExtension.Contains(ext, StringComparison.OrdinalIgnoreCase)) kind = AssetKind.Texture;
        else Throwers.Unreachable(nameof(fileName));
        return true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TestFileName2(ReadOnlySpan<char> fileName, out AssetKind kind)
    {
        kind = AssetKind.Unknown;
        var ext = Path.GetExtension(fileName);
        if (fileName.StartsWith('.') || ext.IsEmpty || ext is ".asset") return false;

        if (ValidShaderExtension.Contains(ext, StringComparison.OrdinalIgnoreCase)) kind = AssetKind.Shader;
        else if (ValidModelExtension.Contains(ext, StringComparison.OrdinalIgnoreCase)) kind = AssetKind.Model;
        else if (ValidTextureExtension.Contains(ext, StringComparison.OrdinalIgnoreCase)) kind = AssetKind.Texture;
        else return false;
        return true;
    }

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