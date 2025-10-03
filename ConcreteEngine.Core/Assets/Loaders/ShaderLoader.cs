#region

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ConcreteEngine.Core.Assets.Manifest;

#endregion

namespace ConcreteEngine.Core.Assets.Loaders;

internal sealed record ShaderPayload(string Vs, string Fs);

internal sealed class ShaderLoader(IReadOnlyList<ShaderManifestRecord> records)
    : AssetTypeLoader<ShaderManifestRecord, ShaderPayload>(records)
{
    private static readonly UTF8Encoding ShaderEncoding = new(false, true);

    private Dictionary<string, string> _vertexShaderCache = new(4, StringComparer.Ordinal);

    private UniformsStd140Layouts _layouts = new();

    public override ShaderPayload ProcessResource(ShaderManifestRecord record, out AssetProcessInfo info)
    {
        var vertPath = Path.Combine(AssetPaths.GetAbsolutePath(), "shaders", record.VertexFilename);
        var fragPath = Path.Combine(AssetPaths.GetAbsolutePath(), "shaders", record.FragmentFilename);

        if (!_vertexShaderCache.TryGetValue(record.VertexFilename, out var vertexSource))
        {
            var rawSource = File.ReadAllText(vertPath, ShaderEncoding);
            vertexSource = ResolveIncludes(rawSource, _layouts);
            _vertexShaderCache[record.VertexFilename] = vertexSource;
        }

        var rawFragSource = File.ReadAllText(fragPath, ShaderEncoding);
        var fragmentSource = ResolveIncludes(rawFragSource, _layouts);
        info = AssetProcessInfo.MakeDone<ShaderManifestRecord>();
        return new ShaderPayload(vertexSource, fragmentSource);
    }

    protected override void ClearCache()
    {
        _vertexShaderCache.Clear();
        _vertexShaderCache.TrimExcess();

        _vertexShaderCache = null!;
        _layouts.Cleanup();
        _layouts = null!;
    }

    private string ToHashSource(string source)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    private static string ResolveIncludes(string source, UniformsStd140Layouts layouts)
    {
        if (string.IsNullOrWhiteSpace(source)) return source;

        //#include(DirLight) 
        var pattern = new Regex(
            @"#include\s*\(\s*(?<name>\w+)\s*\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        const int maxPasses = 8;
        var result = source;

        for (int i = 0; i < maxPasses; i++)
        {
            bool any = false;
            result = pattern.Replace(result, m =>
            {
                any = true;
                var name = m.Groups["name"].Value;

                if (!layouts.Map.TryGetValue(name, out var layout))
                    throw new InvalidOperationException($"No layout mapped for ShaderBufferUniform '{name}'.");

                return layout;
            });

            if (!any) break;
        }

        return result;
    }
}