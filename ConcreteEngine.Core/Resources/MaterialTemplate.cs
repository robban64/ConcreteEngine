using System.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;

namespace ConcreteEngine.Core.Resources;

public sealed class MaterialTemplate : IAssetFile
{
    private readonly Dictionary<ShaderUniform, IMaterialValue> _uniforms;
    public required string Name { get; init; }
    public required Shader Shader { get; set; }
    public required Texture2D[] Textures { get; init; }
    public Vector4 Color { get; set; } = Vector4.One;

    public IReadOnlyDictionary<ShaderUniform, IMaterialValue> DefaultUniforms => _uniforms;

    public AssetFileType AssetType => AssetFileType.Material;

    internal MaterialTemplate(Dictionary<ShaderUniform, IMaterialValue> loadedUniforms)
    {
        _uniforms = loadedUniforms;
    }


    public T Get<T>(ShaderUniform u) where T : struct
    {
        if (!_uniforms.TryGetValue(u, out var mv))
            throw new InvalidOperationException($"Material {Name} does not contain uniform {Enum.GetName(u)}");

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

    public void SetValue<T>(ShaderUniform uniform, T value) where T : struct
    {
        if (!_uniforms.ContainsKey(uniform))
            throw new InvalidOperationException($"Material {Name} does not contain uniform {Enum.GetName(uniform)}");

        var t = typeof(T);
        object v = value;
        if (t == typeof(float))
            _uniforms[uniform] = new MaterialValue<float>((float)v, UniformValueKind.Float);
        else if (t == typeof(int))
            _uniforms[uniform] = new MaterialValue<int>((int)v, UniformValueKind.Int);
        else if (t == typeof(Vector2))
            _uniforms[uniform] = new MaterialValue<Vector2>((Vector2)v, UniformValueKind.Vec2);
        else if (t == typeof(Vector3))
            _uniforms[uniform] = new MaterialValue<Vector3>((Vector3)v, UniformValueKind.Vec3);
        else if (t == typeof(Vector4))
            _uniforms[uniform] = new MaterialValue<Vector4>((Vector4)v, UniformValueKind.Vec4);
        else if (t == typeof(Matrix3x2))
            _uniforms[uniform] = new MaterialValue<Matrix3x2>((Matrix3x2)v, UniformValueKind.Mat3);
        else if (t == typeof(Matrix4x4))
            _uniforms[uniform] = new MaterialValue<Matrix4x4>((Matrix4x4)v, UniformValueKind.Mat4);
        else
            throw new InvalidOperationException($"Invalid uniform type {t.Name}");
    }
}