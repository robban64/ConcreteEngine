using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderShader : IComparable<ShaderId>
{
    public ShaderId Id { get; }
    public int SamplerSlots { get; }

    private Dictionary<string, int>? _uniforms;
    private int[]? _sparse;

    public bool HasPlainUniforms => _uniforms is not null;

    public int GetUniform(string uniformName) => _uniforms![uniformName];
    public int GetUniformByIndex(int idx) => _sparse![idx];

    public ReadOnlySpan<int> GetUniforms() => _sparse;


    internal RenderShader(ShaderId id, ShaderMeta meta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(id.Value, 0, nameof(id));
        Id = id;
        SamplerSlots = meta.SamplerSlots;
    }

    public void UsePlainUniforms(GfxShaders gfx)
    {
        InvalidOpThrower.ThrowIfNotNull(_uniforms, nameof(_uniforms));
        InvalidOpThrower.ThrowIfNotNull(_sparse, nameof(_sparse));

        var uniformPairs = gfx.GetUniformList(Id);
        _uniforms = new Dictionary<string, int>(uniformPairs.Count);
        _sparse = new int[uniformPairs.Count];

        for (int i = 0; i < uniformPairs.Count; i++)
        {
            (string uniform, int location) = uniformPairs[i];
            _uniforms.Add(uniform, location);
            _sparse[i] = location;
        }
    }

    public int CompareTo(ShaderId other) => Id.Value.CompareTo(other.Value);
}