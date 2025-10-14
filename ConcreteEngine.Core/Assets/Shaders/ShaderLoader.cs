#region

using System.Security.Cryptography;
using System.Text;
using ConcreteEngine.Core.Assets.Data;

#endregion

namespace ConcreteEngine.Core.Assets.Shaders;

internal sealed class ShaderLoader
{
    private readonly ShaderImporter _shaderImporter = new();

    public void Prepare()
    {
        _shaderImporter.ImportAllDefinitions();
        //_vertexShaderCache ??= new Dictionary<string, string?>( StringComparer.Ordinal);
        //_vertexShaderCache.Clear();
    }

    public ShaderPayload LoadShader(ShaderManifestRecord record)
    {
        var vertPath = Path.Combine(AssetPaths.GetAssetPath(), "shaders", record.VertexFilename);
        var fragPath = Path.Combine(AssetPaths.GetAssetPath(), "shaders", record.FragmentFilename);

        var vertInfo = new FileInfo(vertPath);
        if (!vertInfo.Exists) throw new FileNotFoundException("File not found.", vertPath);

        var fragInfo = new FileInfo(fragPath);
        if (!fragInfo.Exists) throw new FileNotFoundException("File not found.", fragPath);


        var vertResult = _shaderImporter.ImportShader(vertPath);
        var fragResult = _shaderImporter.ImportShader(fragPath);

        var vertFileSpec = new AssetFileSpec(
            storage: AssetStorageKind.FileSystem,
            logicalName: record.Name,
            relativePath: record.VertexFilename,
            sizeBytes: vertInfo.Length);

        var fragFileSpec = new AssetFileSpec(
            storage: AssetStorageKind.FileSystem,
            logicalName: record.Name,
            relativePath: record.FragmentFilename,
            sizeBytes: fragInfo.Length);


        return new ShaderPayload(vertResult, fragResult, in vertFileSpec, in fragFileSpec);
    }

    public void ClearCache()
    {
        //_vertexShaderCache.Clear();
        //_vertexShaderCache = null!;

        _shaderImporter.ClearCache();
    }

    private string ToHashSource(string source)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
}