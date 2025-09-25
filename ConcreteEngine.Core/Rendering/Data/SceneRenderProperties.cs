using System.Numerics;
using ConcreteEngine.Core.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

public sealed class SceneRenderProperties
{
    private bool _dirty = false;
    private int _version = 0;

    private readonly RenderGlobalSnapshot _currentSnapshot = new();

    private Vector3 _ambient = new Vector3(0.3f, 0.3f, 0.3f);
    private float _exposure = 2.0f;

    private Skybox _skybox;
    private DirectionalLight _directionalLight;

    private Vector2D<int> _outputSize;

    public RenderGlobalSnapshot CurrentSnapshot => _currentSnapshot;

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
        _currentSnapshot.Update(version: _version++,
            outputSize: in _outputSize,
            exposure: _exposure,
            ambient: in _ambient,
            skybox: in _skybox,
            dirLight: in _directionalLight);
        _dirty = false;
    }
}

public sealed class RenderGlobalSnapshot
{
    public DirectionalLight DirLight { get; private set; }
    public Vector3 Ambient { get; private set; }
    public float Exposure { get; private set; }
    public Skybox Skybox { get; private set; }
    public Vector2D<int> OutputSize { get; private set; }
    public int Version { get; private set; }

    internal void Update(in Skybox skybox,
        in DirectionalLight dirLight,
        in Vector2D<int> outputSize,
        in Vector3 ambient,
        float exposure,
        int version)
    {
        Skybox = skybox;
        DirLight = dirLight;
        OutputSize = outputSize;
        Ambient = ambient;
        Exposure = exposure;
        Version = version;
    }
}

public readonly struct Skybox(
    MaterialId materialId,
    Quaternion rotation,
    float intensity = 1)
{
    public readonly Quaternion Rotation = rotation;
    public readonly float Intensity = intensity;
    public readonly MaterialId MaterialId = materialId;
}

public readonly struct DirectionalLight(
    Vector3 direction,
    Vector3 diffuse,
    Vector3 specular,
    float intensity
)
{
    public readonly Vector3 Direction = direction;
    public readonly Vector3 Diffuse = diffuse;
    public readonly Vector3 Specular = specular;
    public readonly float Intensity = intensity;
}