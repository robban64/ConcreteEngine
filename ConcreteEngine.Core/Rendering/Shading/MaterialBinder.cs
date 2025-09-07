using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

internal sealed class ShaderGlobalBindings
{
    public GlobalCameraBindings GlobalCameraBinding { get; }
    public GlobalLightUniformsBindings GlobalLightUniformsBinding { get; }

    public ShaderGlobalBindings(UniformTable uniforms)
    {
        GlobalCameraBinding = new GlobalCameraBindings(
            ViewMat: uniforms.ContainsKey(ShaderUniform.ViewMatrix),
            ProjMat: uniforms.ContainsKey(ShaderUniform.ProjectionMatrix),
            ProjViewMat: uniforms.ContainsKey(ShaderUniform.ProjectionViewMatrix),
            CameraPos: uniforms.ContainsKey(ShaderUniform.CameraPos)
        );

        var hasDirLight = uniforms.ContainsStruct(ShaderStructUniform.DirLight);
        GlobalLightUniformsBinding = new GlobalLightUniformsBindings(
            Ambient:  uniforms.ContainsKey(ShaderUniform.Ambient),
            HasDirLight: hasDirLight,
            DirLight: hasDirLight ? new DirLightUniformStruct(
                Direction: uniforms.GetUniformLocation(DirLightUniformStruct.DirectionUniform),
                Diffuse: uniforms.GetUniformLocation(DirLightUniformStruct.DiffuseUniform),
                Specular: uniforms.GetUniformLocation(DirLightUniformStruct.SpecularUniform),
                Intensity: uniforms.GetUniformLocation(DirLightUniformStruct.IntensityUniform)
            ) : default
        );
    }


    public record struct GlobalCameraBindings(bool ViewMat, bool ProjMat, bool ProjViewMat, bool CameraPos);

    public record struct GlobalLightUniformsBindings(bool Ambient, bool HasDirLight, DirLightUniformStruct DirLight);
}

public class MaterialBindings
{
    private readonly SortedList<ShaderUniform, IMaterialValue> _uniformValues = new(4);
    public IReadOnlyDictionary<ShaderUniform, IMaterialValue> UniformValues => _uniformValues;
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue<T>(ShaderUniform uniform, T value) where T : unmanaged, IMaterialValue
        => _uniformValues[uniform] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet<T>(ShaderUniform u, out T tv) where T : struct
    {
        if (_uniformValues.TryGetValue(u, out var v) && v is T result)
        {
            tv = result;
            return true;
        }

        tv = default;
        return false;
    }
}

internal sealed class MaterialBinder
{
    private readonly IGraphicsDevice _graphics;
    private readonly MaterialStore _materialStore;
    private readonly UniformBinder _uniformBinder;

    private int _previousMaterialId = -1;

    internal MaterialBinder(IGraphicsDevice graphics, MaterialStore materialStore,UniformBinder uniformBinder)
    {
        _graphics = graphics;
        _materialStore = materialStore;
        _uniformBinder = uniformBinder;
    }


    public void Prepare(ICamera camera, in RenderGlobalSnapshot snapshot)
    {
        _previousMaterialId = -1;
        
    }

    public void BindDraw(in DrawObjectUniformRecord rec)
    {
        _uniformBinder.ApplyDrawObject(in rec);
    }


    public void BindMaterialSlots(MaterialId materialId)
    {
        if (_previousMaterialId != -1) _previousMaterialId = materialId.Id;
        if (_previousMaterialId == materialId.Id) return;


        var gfx = _graphics.Gfx;
        var material = _materialStore.GetMaterial(materialId);
        
        _uniformBinder.ApplyMaterial(new MaterialUniformRecord(materialId, Vector3.One, 24, 1));

        gfx.UseShader(material.ShaderId);

        for (int t = 0; t < material.SamplerSlots.Length; t++)
        {
            gfx.BindTexture(material.SamplerSlots[t], (uint)t);
        }
        /*
        foreach (var (uniform, value) in materialBindings.UniformValues)
        {
            switch (value)
            {
                case MatValues.Mat4Val mat4Val:
                    gfx.SetUniform(uniform, in mat4Val.Value);
                    break;
                case MatValues.Vec3Val vec3Val:
                    gfx.SetUniform(uniform, vec3Val.Value);
                    break;
                case MatValues.Mat3Val mat3Val:
                    gfx.SetUniform(uniform, mat3Val.Value);
                    break;
                case MatValues.FloatVal floatVal:
                    gfx.SetUniform(uniform, floatVal.Value);
                    break;
                case MatValues.IntVal intVal:
                    gfx.SetUniform(uniform, intVal.Value);
                    break;
                case MatValues.Vec2Val vec2Val:
                    gfx.SetUniform(uniform, vec2Val.Value);
                    break;
                case MatValues.Vec4Val vec4Val:
                    gfx.SetUniform(uniform, vec4Val.Value);
                    break;

            }
        }*/
    }
}