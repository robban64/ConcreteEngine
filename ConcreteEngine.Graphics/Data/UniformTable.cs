#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Data;

public enum UniformValueKind : byte
{
    Float,
    Int,
    Vec2,
    Vec3,
    Vec4,
    Mat3,
    Mat4
}

public sealed class UniformTable
{
    private static readonly ShaderUniform[] ShaderUniformValues = Enum.GetValues<ShaderUniform>();

    private readonly short[] _locs = new short[ShaderUniformValues.Length];
    private readonly List<ShaderUniform> _uniforms;

    public IReadOnlyList<ShaderUniform> Uniforms => _uniforms;

    public UniformTable(Dictionary<string, short> uniformLocationDict)
    {
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

    public int this[ShaderUniform uniform]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _locs[(int)uniform];
    }
}