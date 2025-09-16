using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public readonly record struct DrawObjectId(int Value);

public readonly record struct ViewId(int Value);

public readonly struct FrameUniformRecord(
    Vector3 ambient,
    float ambientIntensity,
    Vector3 fogColor,
    float fogDensity,
    float fogNear,
    float fogFar,
    float fogType)
{
    public readonly Vector3 Ambient = ambient;
    public readonly Vector3 FogColor = fogColor;
    public readonly float AmbientIntensity = ambientIntensity;
    public readonly float FogDensity = fogDensity;
    public readonly float FogNear = fogNear;
    public readonly float FogFar = fogFar;
    public readonly float FogType = fogType;
}

public readonly struct CameraUniformRecord(
    ViewId viewId,
    in Matrix4x4 viewMat,
    in Matrix4x4 projMat,
    in Matrix4x4 projViewMat,
    Vector3 cameraPos)
{
    public readonly Matrix4x4 ViewMat = viewMat;
    public readonly Matrix4x4 ProjMat = projMat;
    public readonly Matrix4x4 ProjViewMat = projViewMat;
    public readonly Vector3 CameraPos = cameraPos;
    public readonly ViewId ViewId = viewId;
}

public readonly struct DirLightUniformRecord(
    ViewId viewId,
    Vector3 direction,
    Vector3 diffuse,
    Vector3 specular,
    float intensity)
{
    public readonly Vector3 Direction = direction;
    public readonly Vector3 Diffuse = diffuse;
    public readonly Vector3 Specular = specular;
    public readonly float Intensity = intensity;
    public readonly ViewId ViewId = viewId;
}

public readonly struct MaterialUniformRecord(MaterialId Id, Vector3 color, float shininess, float specularStrength, float uvRepeat)
{
    public readonly Vector3 Color = color;
    public readonly float Shininess = shininess;
    public readonly float SpecularStrength = specularStrength;
    public readonly float UvRepeat = uvRepeat;

}
