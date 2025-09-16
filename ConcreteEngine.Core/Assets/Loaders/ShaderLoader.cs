using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Assets.Loaders;

internal sealed class ShaderLoader(IReadOnlyList<ShaderManifestRecord> records) : AssetTypeLoader<ShaderManifestRecord, GpuShaderData>(records)
{
    private static readonly UTF8Encoding ShaderEncoding = new (false, true);
    
    private readonly Dictionary<string, string> _vertexShaderCache = new(StringComparer.Ordinal);
    
    protected override void ClearCache()
    {
        _vertexShaderCache.Clear();
        _vertexShaderCache.TrimExcess();
    }

    public override GpuShaderData Get(ShaderManifestRecord record)
    {
        var vertPath = Path.Combine(AssetPaths.GetAbsolutePath(), "shaders", record.VertexFilename);
        var fragPath = Path.Combine(AssetPaths.GetAbsolutePath(), "shaders", record.FragmentFilename);

        if (!_vertexShaderCache.TryGetValue(record.VertexFilename, out var vertexSource))
        {
            var rawSource = File.ReadAllText(vertPath, ShaderEncoding);
            vertexSource = ResolveIncludes(rawSource);
            _vertexShaderCache[record.VertexFilename] = vertexSource;
        }

        var rawFragSource = File.ReadAllText(fragPath, ShaderEncoding);
        var fragmentSource = ResolveIncludes(rawFragSource);
        return new GpuShaderData(vertexSource, fragmentSource);
    }

    private string ToHashSource(string source)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    private static string ResolveIncludes(string source)
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

                if (!Enum.TryParse<UniformGpuSlot>(name, ignoreCase: true, out var key))
                    throw new InvalidOperationException($"Unknown ShaderBufferUniform '{name}' in include.");

                if (!UniformsStd140Layouts.Map.TryGetValue(key, out var layout))
                    throw new InvalidOperationException($"No layout mapped for ShaderBufferUniform '{key}'.");

                return layout;
            });

            if (!any) break;
        }

        return result;
    }



}