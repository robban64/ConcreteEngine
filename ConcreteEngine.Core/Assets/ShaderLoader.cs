using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Assets;

internal sealed class ShaderLoader : IAssetTypeLoader, IGpuShaderPayloadProvider
{
    private static readonly UTF8Encoding ShaderEncoding = new (false, true);
    
    private readonly IReadOnlyList<ShaderManifestRecord> _records;
    private readonly Dictionary<string, string> _vertexShaderCache = new(StringComparer.Ordinal);
    
    private List<Shader> _results;
    
    internal IReadOnlyList<Shader> Results => _results;
    
    public bool HasStarted { get; private set; }
    public bool IsFinished { get; private set; }

    public ShaderLoader(IReadOnlyList<ShaderManifestRecord> records)
    {
        _records = records;
    }
    
        
    public void CleanCache()
    {
        _vertexShaderCache.Clear();
        _vertexShaderCache.TrimExcess();
        _results.Clear();
        _results.TrimExcess();
    }

    public IReadOnlyList<GpuShaderPayload> Get()
    {
        HasStarted = true;
        var payloads = new List<GpuShaderPayload>();
        foreach (var record in _records)
        {
            payloads.Add(CreatePayload(record));
        }
        return payloads;
    }


    public void Callback(ReadOnlySpan<(ShaderId, ShaderMeta)> result)
    {
        IsFinished = true;
        _results = new List<Shader>(result.Length);
        
        for (int i = 0; i < result.Length; i++)
        {
            var (id, meta) = result[i];
            var record = _records[i];
            
            _results.Add(new Shader
            {
                Name = record.Name,
                FragShaderFilename = record.FragmentFilename,
                VertShaderFilename = record.VertexFilename,
                ResourceId = id,
                Samplers = meta.Samplers
            });
        }
    }
    
 

    private GpuShaderPayload CreatePayload(ShaderManifestRecord record)
    {
        var vertPath = Path.Combine(AssetPaths.AssetPath, "shaders", record.VertexFilename);
        var fragPath = Path.Combine(AssetPaths.AssetPath, "shaders", record.FragmentFilename);

        if (!_vertexShaderCache.TryGetValue(record.VertexFilename, out var vertexSource))
        {
            var rawSource = File.ReadAllText(vertPath, Encoding.UTF8);
            vertexSource = ResolveIncludes(rawSource);
            _vertexShaderCache[record.VertexFilename] = vertexSource;
        }

        var rawFragSource = File.ReadAllText(fragPath, Encoding.UTF8);
        var fragmentSource = ResolveIncludes(rawFragSource);
        return new GpuShaderPayload(vertexSource, fragmentSource);
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