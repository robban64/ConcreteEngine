using System.Numerics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Scene;

public sealed class SceneRenderGlobals
{
    private bool _dirty = false;
    private int _version = 0;

    private Skybox _skybox;
    private SceneRenderGlobalSnapshot _snapshot;
    
    public SceneRenderGlobalSnapshot Snapshot => _snapshot;

    public void SetSkybox(ShaderId shaderId, TextureId cubemapId, Quaternion rotation, float intensity = 1f)
    {
        _skybox = new Skybox(shaderId, cubemapId, rotation, intensity);
        _dirty = true;
    }

    internal void Commit()
    {
        if (!_dirty) return;
        _snapshot = new SceneRenderGlobalSnapshot(
            Version: _version++,
            Skybox: in _skybox
        );
        _dirty = false;
    }
}

public readonly record struct SceneRenderGlobalSnapshot(
    int Version,
    in Skybox Skybox
);

public readonly record struct Skybox(
    ShaderId ShaderId,
    TextureId CubemapId,
    Quaternion Rotation,
    float Intensity = 1);