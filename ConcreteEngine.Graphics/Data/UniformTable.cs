#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Data;

public sealed class UniformTable
{
    private static readonly ShaderUniform[] ShaderUniformValues = Enum.GetValues<ShaderUniform>();

    private readonly Dictionary<string, int> _uniformLocationDict;
    private readonly int[] _locs = new int[ShaderUniformValues.Length];

    public UniformTable(Dictionary<string, int> uniformLocationDict)
    {
        _uniformLocationDict = uniformLocationDict;

        for (int i = 0; i < _locs.Length; i++)
        {
            var uniformName = ShaderUniformValues[i].ToUniformName();
            _locs[i] = _uniformLocationDict.GetValueOrDefault(uniformName, -1);
        }
    }


    public int this[ShaderUniform u]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _locs[(int)u];
    }


    public int GetUniformLocation(string uniform) => _uniformLocationDict[uniform];
}