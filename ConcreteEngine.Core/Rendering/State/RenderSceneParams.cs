#region

using System.Numerics;
using ConcreteEngine.Core.Assets.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.State;

public readonly struct SkyboxParams(MaterialId materialId, Quaternion rotation, float intensity)
{
    public MaterialId MaterialId { get; init; } = materialId;
    public Quaternion Rotation { get; init; } = rotation;
    public float Intensity { get; init; } = intensity;
}

public readonly struct DirLightParams(Vector3 direction, Vector3 diffuse, float intensity, float specular)
{
    public Vector3 Direction { get; init; } = direction;
    public Vector3 Diffuse { get; init; } = diffuse;
    public float Intensity { get; init; } = intensity;
    public float Specular { get; init; } = specular;
}

public readonly struct AmbientParams(Vector3 ambient, Vector3 ambientGround, float exposure)
{
    public Vector3 Ambient { get; init; } = ambient;
    public Vector3 AmbientGround { get; init; } = ambientGround;
    public float Exposure { get; init; } = exposure;
}

public readonly struct FogParams(
    Vector3 color,
    float density,
    float heightFalloff,
    float baseHeight,
    float scattering,
    float maxDistance,
    float heightInfluence,
    float strength
)
{
    public Vector3 Color { get; init; } = color;
    public float Density { get; init; } = density;
    public float HeightFalloff { get; init; } = heightFalloff;
    public float BaseHeight { get; init; } = baseHeight;
    public float Scattering { get; init; } = scattering;
    public float MaxDistance { get; init; } = maxDistance;
    public float HeightInfluence { get; init; } = heightInfluence;
    public float Strength { get; init; } = strength;
}

public readonly struct ShadowParams(
    int shadowMapSize,
    float zPad,
    float constBias,
    float slopeBias,
    float strength,
    float pcfRadius
)
{
    public int ShadowMapSize { get; init; } = shadowMapSize;
    public float ZPad { get; init; } = zPad;
    public float ConstBias { get; init; } = constBias;
    public float SlopeBias { get; init; } = slopeBias;
    public float Strength { get; init; } = strength;
    public float PcfRadius { get; init; } = pcfRadius;

}

