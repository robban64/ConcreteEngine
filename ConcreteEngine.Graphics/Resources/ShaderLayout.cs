#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;

#endregion

namespace ConcreteEngine.Graphics.Resources;


public sealed class ShaderLayout
{
    private readonly int[] _locs;
    private readonly Dictionary<string, int> _rawUniforms;

    public ShaderLayout(List<(string, int)> uniformPairs)
    {
        _locs = new int [GraphicsEnumCache.ShaderUniformVals.Length];
        _rawUniforms = new Dictionary<string, int>(uniformPairs.Count);


        foreach (var (uniform, location) in uniformPairs)
        {
            _rawUniforms.Add(uniform, location);
            var idx = uniform.IndexOf(".", StringComparison.Ordinal);
            if (idx <= 0) continue;
            
            var uniformName = uniform.AsSpan(0, idx);
        }
            
        for (int i = 0; i < _locs.Length; i++)
        {
            var uniformName = GraphicsEnumCache.ShaderUniformVals[i].ToUniformName();
            if (_rawUniforms.TryGetValue(uniformName, out var uniformLocation))
            {
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