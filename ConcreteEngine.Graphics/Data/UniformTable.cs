#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Data;

public sealed class UniformTable
{
    private static readonly ShaderUniform[] ShaderUniformValues = Enum.GetValues<ShaderUniform>();

    private readonly Dictionary<string, short> _uniformLocationDict;
    private readonly short[] _locs = new short[ShaderUniformValues.Length];

    public UniformTable(Dictionary<string, short> uniformLocationDict)
    {
        _uniformLocationDict = uniformLocationDict;

        for (int i = 0; i < _locs.Length; i++)
        {
            var uniformName = ShaderUniformValues[i].ToUniformName();
            _locs[i] = _uniformLocationDict.GetValueOrDefault(uniformName, (short)-1);
        }
    }


    public int this[ShaderUniform u]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _locs[(int)u];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetUniformLocation(string uniform) => _uniformLocationDict[uniform];
}