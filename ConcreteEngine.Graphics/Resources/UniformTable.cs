#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;

#endregion

namespace ConcreteEngine.Graphics.Resources;


public sealed class UniformTable
{
    private static readonly ShaderUniform[] ShaderUniformValues = Enum.GetValues<ShaderUniform>();

    private readonly int[] _locs = new int[ShaderUniformValues.Length];

    private readonly Dictionary<string, int> _rawUniforms;
    
    private readonly List<ShaderUniform> _uniforms;
    private readonly HashSet<ShaderStructUniform> _structUniforms;

    public IReadOnlyList<ShaderUniform> Uniforms => _uniforms;

    public UniformTable(List<(string, int)> uniformPairs)
    {
        _rawUniforms = new Dictionary<string, int>(uniformPairs.Count);
        _uniforms = new List<ShaderUniform>(4);
        _structUniforms = [];


        foreach (var (uniform, location) in uniformPairs)
        {
            _rawUniforms.Add(uniform, location);
            var idx = uniform.IndexOf(".", StringComparison.Ordinal);
            if (idx <= 0) continue;
            
            var uniformName = uniform.AsSpan(0, idx);
            var value = ShaderStructUniforms.ToUniform(uniformName);
            _structUniforms.Add(value);
        }
            
        for (int i = 0; i < _locs.Length; i++)
        {
            var uniformName = ShaderUniformValues[i].ToUniformName();
            if (_rawUniforms.TryGetValue(uniformName, out var uniformLocation))
            {
                _uniforms.Add((ShaderUniform)uniformLocation);
                _locs[i] = uniformLocation;
                continue;
            }

            _locs[i] = -1;
        }

        if (_rawUniforms.ContainsKey(""))
        {
            return;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsStruct(ShaderStructUniform sUniform) => _structUniforms.Contains(sUniform);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(ShaderUniform uniform) => _locs[(int)uniform] >= 0;
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetUniformLocation(string key, int defaultValue = -1)
    {
        return _rawUniforms.TryGetValue(key, out var uniformLocation) ? uniformLocation : defaultValue;
    }

    public int this[ShaderUniform uniform]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _locs[(int)uniform];
    }
}