using System.Numerics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public readonly struct GlobalUniformValues(
    in GlobalCameraUniformValues cameraUniformValues,
    in GlobalLightUniformValues lightUniformValues
)
{
    public readonly GlobalCameraUniformValues CameraUniformValues = cameraUniformValues;
    public readonly GlobalLightUniformValues LightUniformValues = lightUniformValues;
}

public readonly struct GlobalCameraUniformValues(
    in Matrix4x4 viewMat,
    in Matrix4x4 projMat,
    in Matrix4x4 projViewMat,
    Vector3 cameraPos)
{
    public readonly Matrix4x4 ViewMat = viewMat;
    public readonly Matrix4x4 ProjMat = projMat;
    public readonly Matrix4x4 ProjViewMat = projViewMat;
    public readonly Vector3 CameraPos = cameraPos;
}

public readonly struct GlobalLightUniformValues(
    Vector3 ambient,
    in DirLightUniformValues dirLight)
{
    public readonly Vector3 Ambient  = ambient;
    public readonly DirLightUniformValues DirLight = dirLight;
}

public readonly struct DirLightUniformValues(
    Vector3 direction,
    Vector3 diffuse,
    Vector3 specular,
    float intensity)
{
    public readonly Vector3 Direction  = direction;
    public readonly Vector3 Diffuse  = diffuse;
    public readonly Vector3 Specular  = specular;
    public readonly float Intensity  = intensity;

}
    
