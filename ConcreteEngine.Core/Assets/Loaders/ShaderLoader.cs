#region

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ConcreteEngine.Core.Assets.Importers;
using ConcreteEngine.Core.Assets.Manifest;

#endregion

namespace ConcreteEngine.Core.Assets.Loaders;

internal sealed record ShaderPayload(string Vs, string Fs);

internal sealed class ShaderLoader(IReadOnlyList<ShaderManifestRecord> records)
    : AssetTypeLoader<ShaderManifestRecord, ShaderPayload>(records)
{

    private readonly ShaderImporter _shaderImporter = new();

    //private Dictionary<string, string?>? _vertexShaderCache;

    public override void Prepare()
    {
        _shaderImporter.LoadDefinitions();
        //_vertexShaderCache ??= new Dictionary<string, string?>( StringComparer.Ordinal);
        //_vertexShaderCache.Clear();
    }
    
    public override ShaderPayload ProcessResource(ShaderManifestRecord record, out AssetProcessInfo info)
    {
        var vertPath = Path.Combine(AssetPaths.GetAssetPath(), "shaders", record.VertexFilename);
        var fragPath = Path.Combine(AssetPaths.GetAssetPath(), "shaders", record.FragmentFilename);

        var vertFinal = _shaderImporter.ParseShader(vertPath);
        var fragFinal = _shaderImporter.ParseShader(fragPath);

        info = AssetProcessInfo.MakeDone<ShaderManifestRecord>();
        return new ShaderPayload(vertFinal, fragFinal);
    }
    
    protected override void ClearCache()
    {
        //_vertexShaderCache.Clear();
        //_vertexShaderCache = null!;
        
        _shaderImporter.CleanCache();
    }

    private string ToHashSource(string source)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

}