#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Data;

public sealed class SceneRenderProperties
{
    private bool _dirty = false;
    private int _version = 0;

    private readonly RenderGlobalSnapshot _snapshot = new();

    private Vector3 _ambient = new Vector3(0.3f, 0.3f, 0.3f);
    private float _exposure = 2.0f;

    private Skybox _skybox;
    private DirectionalLight _directionalLight;

    private Size2D _outputSize;

    public RenderGlobalSnapshot Snapshot => _snapshot;

    public void SetOutputSize(Size2D outputSize)
    {
        _outputSize = outputSize;
        _dirty = true;
    }

    public void SetAmbient(Vector3 ambient)
    {
        _ambient = ambient;
        _dirty = true;
    }

    public void SetSkybox(MaterialId materialId, Quaternion rotation, float intensity = 1f)
    {
        _skybox = new Skybox(materialId, rotation, intensity);
        _dirty = true;
    }

    public void SetDirLight(Vector3 direction,
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

    internal RenderGlobalSnapshot Commit()
    {
        if (!_dirty) return _snapshot;

        _snapshot.Update(version: _version++,
            outputSize: _outputSize,
            exposure: _exposure,
            ambient: _ambient,
            skybox: in _skybox,
            dirLight: in _directionalLight);
        _dirty = false;

        return _snapshot;
    }
}

public sealed class RenderGlobalSnapshot
{
    public DirectionalLight DirLight { get; private set; }
    public Vector3 Ambient { get; private set; }
    public float Exposure { get; private set; }
    public Skybox Skybox { get; private set; }
    public Size2D OutputSize { get; private set; }
    public int Version { get; private set; }

    internal void Update(in Skybox skybox,
        in DirectionalLight dirLight,
        Size2D outputSize,
        Vector3 ambient,
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