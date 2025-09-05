using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;

internal sealed class MaterialBinder
{
    private readonly IGraphicsDevice _graphics;
    private readonly MaterialStore _materialStore;

    private int _previousMaterialId = -1;

    internal MaterialBinder(IGraphicsDevice graphics, MaterialStore materialStore)
    {
        _graphics = graphics;
        _materialStore = materialStore;
    }

    public void ClearPreviousMaterial()
    {
        _previousMaterialId = -1;
    }

    public void BindMaterialSlots(MaterialId materialId)
    {
        if (_previousMaterialId != -1) _previousMaterialId = materialId.Id;
        if (_previousMaterialId == materialId.Id) return;

        var gfx = _graphics.Gfx;
        var material = _materialStore.GetMaterial(materialId);
        gfx.UseShader(material.ShaderId);
        for (int t = 0; t < material.SamplerSlots.Length; t++)
        {
            gfx.BindTexture(material.SamplerSlots[t], (uint)t);
        }

        var properties = material.GetProperties();
        foreach (var property in properties)
        {
            var uniform = property.Uniform;
            var kind = property.Kind;

            if (!material.TryGetValue(uniform, out var mv)) continue;

            switch (kind)
            {
                case UniformValueKind.Float:
                    if (mv is MaterialValue<float> f) gfx.SetUniform(uniform, f.Value);
                    break;
                case UniformValueKind.Int:
                    if (mv is MaterialValue<int> i) gfx.SetUniform(uniform, i.Value);
                    break;
                case UniformValueKind.Vec2:
                    if (mv is MaterialValue<Vector2> v2) gfx.SetUniform(uniform, v2.Value);
                    break;
                case UniformValueKind.Vec3:
                    if (mv is MaterialValue<Vector3> v3) gfx.SetUniform(uniform, v3.Value);
                    break;
                case UniformValueKind.Vec4:
                    if (mv is MaterialValue<Vector4> v4) gfx.SetUniform(uniform, v4.Value);
                    break;
            }
        }
    }
}