using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Extensions;

namespace ConcreteEngine.Graphics.Resources;

public enum UniformGpuSlot
{
    Frame = 0,
    Camera = 1,
    DirLight = 2,
    Material = 3,
    DrawObject = 4
}

public interface IUniformGpuData;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FrameUniformGpuData(
    Vector3 ambient,
    float ambientIntensity,
    Vector3 fogColor,
    float fogDensity,
    float fogNear,
    float fogFar,
    float fogType) : IUniformGpuData
{
    public readonly Vector4 Ambient = new(ambient, ambientIntensity);
    public readonly Vector4 FogColor = new(fogColor, fogDensity);
    public readonly Vector4 FogDetail = new(fogNear, fogFar, fogType, 0);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct CameraUniformGpuData(
    in Matrix4x4 viewMat,
    in Matrix4x4 projMat,
    in Matrix4x4 projViewMat,
    Vector3 cameraPos) : IUniformGpuData
{
    public readonly Matrix4x4 ViewMat = viewMat;
    public readonly Matrix4x4 ProjMat = projMat;
    public readonly Matrix4x4 ProjViewMat = projViewMat;
    public readonly Vector3 CameraPos = cameraPos;
    private readonly float _pad0;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DirLightUniformGpuData(
    Vector3 direction,
    Vector3 diffuse,
    Vector3 specular,
    float intensity) : IUniformGpuData
{
    public readonly Vector4 Direction = direction.AsVector4();
    public readonly Vector4 Diffuse = diffuse.AsVector4();
    public readonly Vector4 SpecularIntensity = new(specular, intensity);

    public Vector3 Specular => SpecularIntensity.AsVector3();
    public float Intensity => SpecularIntensity.W;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct MaterialUniformGpuData(
    Vector3 color,
    float shininess,
    float specularStrength,
    float uvRepeat
) : IUniformGpuData
{
    public readonly Vector3 Color = color;
    public readonly float Shininess = shininess;
    public readonly float SpecularStrength = specularStrength;
    public readonly float uvRepeat = uvRepeat;
    private readonly Vector2 _pad0 = default;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawObjectUniformGpuData(
    in Matrix4x4 model,
    in Matrix3 normal
) : IUniformGpuData
{
    public readonly Matrix4x4 Model = model;

    public readonly Vector4 NormalCol0 = new(
        normal.M11, normal.M21, normal.M31, 0f);

    public readonly Vector4 NormalCol1 = new(
        normal.M12, normal.M22, normal.M32, 0f);

    public readonly Vector4 NormalCol2 = new(
        normal.M13, normal.M23, normal.M33, 0f);
}