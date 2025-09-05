using System.Numerics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Scene;

public sealed class SceneRenderGlobals
{
    private bool _dirty = false;
    private int _version = 0;

    private RenderGlobalSnapshot _snapshot;

    private Vector3 _ambient = new Vector3(0.3f, 0.3f, 0.3f);
    private float _exposure = 2.0f;

    private Skybox _skybox;
    private DirectionalLight _directionalLight;

    public RenderGlobalSnapshot Snapshot => _snapshot;

    public void SetAmbient(Vector3 ambient)
    {
        _ambient = ambient;
        _dirty = true;
    }

    public void SetSkybox(ShaderId shaderId, TextureId cubemapId, Quaternion rotation, float intensity = 1f)
    {
        _skybox = new Skybox(shaderId, cubemapId, rotation, intensity);
        _dirty = true;
    }

    public void SetDirLight(Vector3 direction,
        Vector3 diffuse,
        Vector3 specular, float intensity = 1f)
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
            _exposure,
            _ambient,
            Skybox: in _skybox,
            in _directionalLight
        );
        _dirty = false;
    }
}

public readonly record struct RenderGlobalSnapshot(
    int Version,
    float Exposure,
    Vector3 Ambient,
    in Skybox Skybox,
    in DirectionalLight DirLight
);

public readonly record struct Skybox(
    ShaderId ShaderId,
    TextureId CubemapId,
    Quaternion Rotation,
    float Intensity = 1);

public readonly record struct DirectionalLight(
    Vector3 Direction,
    Vector3 Diffuse,
    Vector3 Specular,
    float Intensity
);