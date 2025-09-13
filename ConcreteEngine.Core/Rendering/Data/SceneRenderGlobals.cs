using System.Numerics;
using ConcreteEngine.Core.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

public sealed class SceneRenderGlobals
{
    private bool _dirty = false;
    private int _version = 0;

    private RenderGlobalSnapshot _snapshot;

    private Vector3 _ambient = new Vector3(0.3f, 0.3f, 0.3f);
    private float _exposure = 2.0f;

    private Skybox _skybox;
    private DirectionalLight _directionalLight;

    private Vector2D<int> _outputSize;

    public RenderGlobalSnapshot Snapshot => _snapshot;

    public void SetOutputSize(in Vector2D<int> outputSize)
    {
        _outputSize = outputSize;
        _dirty = true;
    }

    public void SetAmbient(in Vector3 ambient)
    {
        _ambient = ambient;
        _dirty = true;
    }

    public void SetSkybox(MaterialId materialId, Quaternion rotation, float intensity = 1f)
    {
        _skybox = new Skybox(materialId, rotation, intensity);
        _dirty = true;
    }

    public void SetDirLight(in Vector3 direction,
        in Vector3 diffuse,
        in Vector3 specular, float intensity = 1f)
    {
        _directionalLight = new DirectionalLight(direction, diffuse, specular, intensity);
        _dirty = true;
    }

    public void SetExposure(float exposure)
    {
        _exposure = MathF.Max(0.0f, exposure);
        _dirty = true;
    }

    internal void Commit()
    {
        if (!_dirty) return;
        _snapshot = new RenderGlobalSnapshot(
            Version: _version++,
            OutputSize: in _outputSize,
            Exposure: _exposure,
            Ambient: _ambient,
            Skybox: in _skybox,
            DirLight: in _directionalLight
        );
        _dirty = false;
    }
}

public readonly record struct RenderGlobalSnapshot(
    in Skybox Skybox,
    in DirectionalLight DirLight,
    in Vector2D<int> OutputSize,
    Vector3 Ambient,
    float Exposure,
    int Version
);

public readonly record struct Skybox(
    MaterialId MaterialId,
    Quaternion Rotation,
    float Intensity = 1);

public readonly record struct DirectionalLight(
    Vector3 Direction,
    Vector3 Diffuse,
    Vector3 Specular,
    float Intensity
);