using System.Security.Cryptography;

namespace ConcreteEngine.Engine.Configuration.IO;

internal static class FileUtils
{
    public static readonly string[] ValidTextureExt = [".png", ".jpg", ".jpeg", ".tga", ".bmp"];
    public static readonly string[] ValidModelExt = [".glb", ".gltf", ".fbx", ".obj"];
    public static readonly string[] ValidShaderExt = [".glsl"];

    public static string ComputeSha256Hex(string path)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(path);
        var hash = sha.ComputeHash(fs);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}