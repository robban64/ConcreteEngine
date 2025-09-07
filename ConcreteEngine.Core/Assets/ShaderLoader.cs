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

                if (!Enum.TryParse<ShaderBufferUniform>(name, ignoreCase: true, out var key))
                    throw new InvalidOperationException($"Unknown ShaderBufferUniform '{name}' in include.");

                if (!ShaderUniformLayouts.Map.TryGetValue(key, out var layout))
                    throw new InvalidOperationException($"No layout mapped for ShaderBufferUniform '{key}'.");

                return layout;
            });

            if (!any) break;
        }

        return result;
    }

    private static class ShaderUniformLayouts
    {
        public static readonly IReadOnlyDictionary<ShaderBufferUniform, string> Map =
            new Dictionary<ShaderBufferUniform, string>
            {
                { ShaderBufferUniform.Frame, FrameGlobalUniform },
                { ShaderBufferUniform.Camera, CameraUniform },
                { ShaderBufferUniform.DirLight, DirLightUniform },
                { ShaderBufferUniform.Material, MaterialUniform },
                { ShaderBufferUniform.DrawObject, DrawUniform },
            };

        private const string FrameGlobalUniform =
            """
            layout(std140, binding = 0) uniform FrameGlobalUniform {
                vec4 uAmbient;   // xyz=color, w=intensity
                vec4 uFogColor;  // xyz=color, w=density
                vec4 uFogDetail; // x=near, y=far, z=type, w=0
            };
            """;

        private const string CameraUniform =
            """
            layout(std140, binding = 1) uniform CameraUniform {
                mat4 uViewMat;
                mat4 uProjMat;
                mat4 uProjViewMat;
                vec4 uCameraPos; // C# has vec3 + float pad; use .xyz
            };
            """;

        private const string DirLightUniform =
            """
            layout(std140, binding = 2) uniform DirLightUniform {
                vec4 uLightDirection;            // xyz, w unused
                vec4 uLightDiffuse;              // rgb, w unused
                vec4 uLightSpecularIntensity;    // xyz=specular, w=intensity
            };
            """;

        private const string MaterialUniform =
            """
            layout(std140, binding = 3) uniform MaterialUniform {
                vec3 MaterialColor;
                float Shininess;
                float SpecularStrength;
                float uvRepeat;                
                vec2 _materialPad0;
            };
            """;

        private const string DrawUniform =
            """
            layout(std140, binding = 4) uniform DrawUniform {
                mat4 uModel;
                // normal matrix as vec4 (xyz used)
                vec4 uNormalCol0;
                vec4 uNormalCol1;
                vec4 uNormalCol2;
            };
            """;
    }
}