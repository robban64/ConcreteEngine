using System.Security.Cryptography;
using ConcreteEngine.Core.Assets.Data;

namespace ConcreteEngine.Core.Assets.IO;

internal sealed class FileUtility
{
    public static string ComputeSha256Hex(string path)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(path);
        var hash = sha.ComputeHash(fs);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}