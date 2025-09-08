using System.Text.RegularExpressions;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Assets;

internal sealed class ShaderLoader
{
    private readonly Dictionary<string, string> _vertexShaderCache = new(StringComparer.Ordinal);

    public void CleanCache()
    {
        _vertexShaderCache.Clear();
    }

    public Shader LoadShader(AssetShaderRecord record, IGraphicsDevice graphics, string vertexPath, string fragmentPath)
    {
        if (!_vertexShaderCache.TryGetValue(record.VertexFilename, out var vertexSource))
        {
            var rawSource = File.ReadAllText(vertexPath);
            vertexSource = ResolveIncludes(rawSource);
            _vertexShaderCache[record.VertexFilename] = vertexSource;
        }

        var rawFragSource = File.ReadAllText(fragmentPath);
        var fragmentSource = ResolveIncludes(rawFragSource);

        var resourceId = graphics.CreateShader(vertexSource, fragmentSource, out var meta);

        return new Shader
        {
            Name = record.Name,
            VertShaderFilename = record.VertexFilename,
            FragShaderFilename = record.FragmentFilename,
            ResourceId = resourceId,
            Samplers = meta.Samplers,
        };
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