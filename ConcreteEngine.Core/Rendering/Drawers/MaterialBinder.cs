using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;

internal sealed class MaterialBinder
{
    private readonly IGraphicsDevice _graphics;
    private readonly MaterialStore _materialStore;

    private int _previousMaterialId = -1;
    
    private RenderGlobalSnapshot _renderGlobals;

    internal MaterialBinder(IGraphicsDevice graphics, MaterialStore materialStore)
    {
        _graphics = graphics;
        _materialStore = materialStore;
    }


    public void Prepare(in RenderGlobalSnapshot renderGlobals)
    {
        _previousMaterialId = -1;
        _renderGlobals =  renderGlobals;
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

        if (material.HasAmbient)
        {
            gfx.SetUniform(ShaderUniform.Ambient,  _renderGlobals.Ambient);
        }
        
        if (material.MaterialUniforms.HasValue)
        {
            var unforms = material.MaterialUniforms.Value;
            gfx.SetRawUniform(unforms.Shininess, material.Shininess);
            gfx.SetRawUniform(unforms.SpecularStrength, material.SpecularStrength);
        }
        
        /*
        if (material.DirLightUniforms.HasValue)
        {
            var unforms = material.DirLightUniforms.Value;
            gfx.SetRawUniform(unforms.Direction, _renderGlobals.DirLight.Direction );
            gfx.SetRawUniform(unforms.Diffuse,  _renderGlobals.DirLight.Diffuse );
            gfx.SetRawUniform(unforms.Specular,  _renderGlobals.DirLight.Specular );
            gfx.SetRawUniform(unforms.Intensity,  _renderGlobals.DirLight.Intensity );
        }
        */

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