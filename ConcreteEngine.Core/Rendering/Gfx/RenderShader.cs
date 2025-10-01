#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

#endregion

namespace ConcreteEngine.Core.Rendering.Gfx;

public sealed class RenderShader
{
    public ShaderId Id { get; }
    private readonly int[] _locations;
    private readonly Dictionary<string, int> _uniforms;

    public IReadOnlyDictionary<string, int> Uniforms => _uniforms;
    public int[] Locations => _locations;

    public RenderShader(ShaderId id, List<(string, int)> uniformPairs)
    {
        Id = id;
        _locations = new int [GraphicsEnumCache.ShaderUniformVals.Length];
        _uniforms = new Dictionary<string, int>(uniformPairs.Count);

        foreach (var (uniform, location) in uniformPairs)
        {
            _uniforms.Add(uniform, location);
            var idx = uniform.IndexOf(".", StringComparison.Ordinal);
        }

        for (int i = 0; i < _locations.Length; i++)
        {
            var uniformName = GraphicsEnumCache.ShaderUniformVals[i].ToUniformName();
            if (_uniforms.TryGetValue(uniformName, out var uniformLocation))
            {
                _locations[i] = uniformLocation;
                continue;
            }

            _locations[i] = -1;
        }
    }

    public int this[ShaderUniform uniform]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _locations[(int)uniform];
    }
}