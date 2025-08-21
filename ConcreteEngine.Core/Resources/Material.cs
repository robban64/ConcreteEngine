#region

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Resources;

public readonly struct MaterialValueProperty(ShaderUniform uniform, UniformValueKind kind)
    : IComparable<MaterialValueProperty>
{
    public readonly ShaderUniform Uniform = uniform;
    public readonly UniformValueKind Kind = kind;

    public int CompareTo(MaterialValueProperty other)
    {
        return Uniform.CompareTo(other.Uniform);
    }
}

public sealed class Material
{
    private readonly Dictionary<ShaderUniform, IMaterialValue> _uniforms;
    private readonly MaterialValueProperty[] _properties;
    private readonly TextureId[] _samplerSlots = [];

    public MaterialId Id { get; }
    public MaterialTemplate Template { get; }
    public ShaderId ShaderId { get; set; }
    public TextureId[] SamplerSlots => _samplerSlots;
    public Vector4 Color { get; set; } = Vector4.One;

    public IReadOnlyDictionary<ShaderUniform, IMaterialValue> UniformDict => _uniforms;
    public IReadOnlyList<ShaderUniform> ActiveUniforms => Template.Shader.UniformTable.Uniforms;

    
    // Helpers
    public bool HasViewProjection { get; private set; }
    //public bool HasModelMatrix { get; private set; }
    //public bool HasModelMatrix { get; private set; }

    
    internal Material(MaterialId id, MaterialTemplate template)
    {
        var defaultUniforms = template.DefaultUniforms;
        var shaderUniforms = template.Shader.Uniforms;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(defaultUniforms.Count, shaderUniforms.Count,
            nameof(defaultUniforms));

        Id = id;
        Template = template;
        ShaderId = template.Shader.ResourceId;
        Color = template.Color;

        _uniforms = new Dictionary<ShaderUniform, IMaterialValue>(4);


        foreach (var uniform in defaultUniforms)
        {
            _uniforms.Add(uniform.Key, uniform.Value);

        }
        _properties = new MaterialValueProperty[_uniforms.Count];

        int idx = 0;
        foreach (var (uniform, value) in _uniforms)
        {
            _properties[idx++] = new MaterialValueProperty(uniform, value.Kind);
        }

        Array.Sort(_properties);


        if (template.Shader.Samplers > 0)
        {
            _samplerSlots = new TextureId[template.Shader.Samplers];
            if (template.Textures?.Length > 0)
            {
                for (int i = 0; i < template.Shader.Samplers; i++)
                    SamplerSlots[i] = template.Textures[i].ResourceId;
            }
        }

        HasViewProjection = template.Shader.UniformTable.ContainsKey(ShaderUniform.ProjectionViewMatrix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<MaterialValueProperty> GetProperties() => _properties;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(ShaderUniform u, out IMaterialValue mv) => _uniforms.TryGetValue(u, out mv);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>(ShaderUniform u) where T : struct
    {
        if (!_uniforms.TryGetValue(u, out var mv)) return default;

        if (typeof(T) == typeof(int) && mv is MaterialValue<int> mi) return (T)(object)mi.Value;
        if (typeof(T) == typeof(float) && mv is MaterialValue<float> mf) return (T)(object)mf.Value;
        if (typeof(T) == typeof(Vector3) && mv is MaterialValue<Vector3> m3) return (T)(object)m3.Value;

        // Optional widenings
        if (typeof(T) == typeof(Vector2) && mv is MaterialValue<Vector3> m3b)
            return (T)(object)new Vector2(m3b.Value.X, m3b.Value.Y);
        if (typeof(T) == typeof(Vector4) && mv is MaterialValue<Vector3> m3c)
        {
            var v3 = m3c.Value;
            return (T)(object)new Vector4(v3, 0f);
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFloat(ShaderUniform uniform, float value)
        => _uniforms[uniform] = new MaterialValue<float>(value, UniformValueKind.Float);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetInt(ShaderUniform uniform, int value)
        => _uniforms[uniform] = new MaterialValue<int>(value, UniformValueKind.Int);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVec2(ShaderUniform uniform, Vector2 value)
        => _uniforms[uniform] = new MaterialValue<Vector2>(value, UniformValueKind.Vec2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVec3(ShaderUniform uniform, Vector3 value)
        => _uniforms[uniform] = new MaterialValue<Vector3>(value, UniformValueKind.Vec3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVec4(ShaderUniform uniform, Vector4 value)
        => _uniforms[uniform] = new MaterialValue<Vector4>(value, UniformValueKind.Vec4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMat3(ShaderUniform uniform, Matrix3x2 value)
        => _uniforms[uniform] = new MaterialValue<Matrix3x2>(value, UniformValueKind.Mat3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMat4(ShaderUniform uniform, in Matrix4x4 value)
        => _uniforms[uniform] = new MaterialValue<Matrix4x4>(in value, UniformValueKind.Mat4);

    [DoesNotReturn]
    private static void ThrowUniformWrongType(ShaderUniform u, Type requested, UniformValueKind actual)
    {
        throw new InvalidOperationException(
            $"Shader uniform '{u}' has kind {actual}, request was {requested.Name}.");
    }
    /*
    public float GetFloat(ShaderUniform u)
    {
        if (_uniforms.TryGetValue(u, out var mv) && mv is FloatValue f) return f.Value;
        ThrowUniformNotFound(u);
        return 0f;
    }

    public int GetInt(ShaderUniform u)
    {
        if (_uniforms.TryGetValue(u, out var mv) && mv is IntValue i) return i.Value;
        ThrowUniformNotFound(u);
        return 0;
    }

    public Vector2 GetVec2(ShaderUniform u)
    {
        if (_uniforms.TryGetValue(u, out var mv) && mv is VecValue vv)
            return new Vector2(vv.Value.X, vv.Value.Y);
        ThrowUniformNotFound(u);
        return default;
    }

    public Vector3 GetVec3(ShaderUniform u)
    {
        if (_uniforms.TryGetValue(u, out var mv) && mv is VecValue vv)
            return new Vector3(vv.Value.X, vv.Value.Y, vv.Value.Z);
        ThrowUniformNotFound(u);
        return default;
    }

    public Vector4 GetVec4(ShaderUniform u)
    {
        if (_uniforms.TryGetValue(u, out var mv) && mv is VecValue vv)
            return vv.Value;
        ThrowUniformNotFound(u);
        return default;
    }

    /// Returns a Matrix4x4 for both Mat3 and Mat4 (top-left 3x3 meaningful for Mat3).
    public Matrix4x4 GetMatrix(ShaderUniform u)
    {
        if (_uniforms.TryGetValue(u, out var mv) && mv is MatValue mm) return mm.M;
        ThrowUniformNotFound(u);
        return default;
    }
    */
}