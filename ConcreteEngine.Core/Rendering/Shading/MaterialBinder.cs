using System.Numerics;
using System.Runtime.CompilerServices;
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

    private int _previousMaterialId = -1;

    private GlobalUniformValues _globalUniformValues;

    private Dictionary<ShaderId, ShaderGlobalBindings> _shaderBindingMap = new();
    private Dictionary<MaterialId, MaterialBindings> _materialBindingMap = new();

    internal MaterialBinder(IGraphicsDevice graphics, MaterialStore materialStore)
    {
        _graphics = graphics;
        _materialStore = materialStore;
    }


    public void Prepare(ICamera camera, in RenderGlobalSnapshot snapshot)
    {
        _previousMaterialId = -1;
        
        var cameraUniforms = new GlobalCameraUniformValues(
            viewMat: camera.ViewMatrix,
            projMat: camera.ProjectionMatrix,
            projViewMat: camera.ProjectionViewMatrix,
            cameraPos: camera.Translation
        );

        var lightUniforms = new GlobalLightUniformValues(
            ambient: snapshot.Ambient,
            dirLight: new DirLightUniformValues(
                direction: snapshot.DirLight.Direction,
                diffuse:snapshot.DirLight.Diffuse,
                specular:snapshot.DirLight.Specular,
                intensity: snapshot.DirLight.Intensity
            )
        );
        
        _globalUniformValues = new GlobalUniformValues(in cameraUniforms, in lightUniforms);
    }

    public void BindGlobalSlots(ShaderId shaderId)
    {
        var gfx = _graphics.Gfx;

        if (!_shaderBindingMap.TryGetValue(shaderId, out var shaderBindings))
        {
            var table = _graphics.GetShaderUniforms(shaderId);
            _shaderBindingMap[shaderId] = shaderBindings = new ShaderGlobalBindings(table);
        }

        gfx.UseShader(shaderId);

        var cb = shaderBindings.GlobalCameraBinding;
        var cv = _globalUniformValues.CameraUniformValues;
        if(cb.ViewMat)
            gfx.SetUniform(ShaderUniform.ViewMatrix, in cv.ViewMat);
        if(cb.ProjMat)
            gfx.SetUniform(ShaderUniform.ProjectionMatrix, in cv.ProjMat);
        if(cb.ProjViewMat)
            gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, in cv.ProjViewMat);
        if(cb.CameraPos)
            gfx.SetUniform(ShaderUniform.CameraPos, in cv.ProjMat);
        
        var lb = shaderBindings.GlobalLightUniformsBinding;
        var cl = _globalUniformValues.LightUniformValues;
        
        if(lb.Ambient)
            gfx.SetUniform(ShaderUniform.Ambient, cl.Ambient);
        
        if (lb.HasDirLight)
        {
            var dirLight = _globalUniformValues.LightUniformValues.DirLight;
            gfx.SetRawUniform(lb.DirLight.Direction, dirLight.Direction );
            gfx.SetRawUniform(lb.DirLight.Diffuse,  dirLight.Diffuse );
            gfx.SetRawUniform(lb.DirLight.Specular,  dirLight.Specular );
            gfx.SetRawUniform(lb.DirLight.Intensity,  dirLight.Intensity );

        }
        
    }

    public void BindMaterialSlots(MaterialId materialId)
    {
        if (_previousMaterialId != -1) _previousMaterialId = materialId.Id;
        if (_previousMaterialId == materialId.Id) return;

        if (!_materialBindingMap.TryGetValue(materialId, out var materialBindings))
            _materialBindingMap[materialId] = materialBindings = new MaterialBindings();


        var gfx = _graphics.Gfx;
        var material = _materialStore.GetMaterial(materialId);

        gfx.UseShader(material.ShaderId);

        for (int t = 0; t < material.SamplerSlots.Length; t++)
        {
            gfx.BindTexture(material.SamplerSlots[t], (uint)t);
        }


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
        }
    }
}