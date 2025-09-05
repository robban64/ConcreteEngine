#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Graphics.Resources;


public sealed class UniformTable
{
    private static readonly ShaderUniform[] ShaderUniformValues = Enum.GetValues<ShaderUniform>();

    private readonly short[] _locs = new short[ShaderUniformValues.Length];
    private readonly List<ShaderUniform> _uniforms;

    private readonly Dictionary<string, short> _rawUniforms;

    public IReadOnlyList<ShaderUniform> Uniforms => _uniforms;

    public UniformTable(Dictionary<string, short> uniformLocationDict)
    {
        _rawUniforms = uniformLocationDict;
        _uniforms = new List<ShaderUniform>(uniformLocationDict.Count);
        _uniforms.Sort();

        for (int i = 0; i < _locs.Length; i++)
        {
            var uniformName = ShaderUniformValues[i].ToUniformName();
            if (uniformLocationDict.TryGetValue(uniformName, out var uniformLocation))
            {
                _uniforms.Add((ShaderUniform)uniformLocation);
                _locs[i] = uniformLocation;
                continue;
            }

            _locs[i] = -1;
        }
    }

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