#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Core.Rendering.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly struct LightDataStruct(
    Vector4 colorIntensity,
    Vector4 posRange,
    Vector4 dirType, 
    Vector4 spotAngles
)
{
    public readonly Vector4 ColorIntensity = colorIntensity;
    public readonly Vector4 PosRange = posRange;
    public readonly Vector4 DirType = dirType;
    public readonly Vector4 SpotAngles = spotAngles;
}


[StructLayout(LayoutKind.Sequential)]
public readonly struct LightUniformRecord(
    int lightCounts,
    LightDataStruct l0,
    LightDataStruct l1= default,
    LightDataStruct l2= default,
    LightDataStruct l3= default,
    LightDataStruct l4= default,
    LightDataStruct l5= default,
    LightDataStruct l6= default,
    LightDataStruct l7 = default) : IStd140Uniform
{
    // yzw unused/padding
    public readonly IVec4Std140 LightCounts = new (lightCounts);

    public readonly LightDataStruct L0 = l0;
    public readonly LightDataStruct L1 = l1;
    public readonly LightDataStruct L2 = l2;
    public readonly LightDataStruct L3 = l3;
    public readonly LightDataStruct L4 = l4;
    public readonly LightDataStruct L5 = l5;
    public readonly LightDataStruct L6 = l6;
    public readonly LightDataStruct L7 = l7;
}


[StructLayout(LayoutKind.Sequential)]
public readonly struct FrameUniformRecord(
    Vector4 ambient,
    Vector4 ambientGround,
    Vector4 fogColor,
    Vector4 fogParams0,
    Vector4 fogParams1) : IStd140Uniform
{
    public readonly Vector4 Ambient = ambient;
    public readonly Vector4 AmbientGround = ambientGround;
    public readonly Vector4 FogColor = fogColor;
    public readonly Vector4 FogParams0 = fogParams0;
    public readonly Vector4 FogParams1 = fogParams1;
}

public readonly struct CameraUniformRecord(
    in Matrix4x4 viewMat,
    in Matrix4x4 projMat,
    in Matrix4x4 projViewMat,
    Vector3 cameraPos) : IStd140Uniform
{
    public readonly Matrix4x4 ViewMat = viewMat;
    public readonly Matrix4x4 ProjMat = projMat;
    public readonly Matrix4x4 ProjViewMat = projViewMat;
    public readonly Vector3 CameraPos = cameraPos;
    private readonly float _pad0;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DirLightUniformRecord(
    Vector3 direction,
    Vector3 diffuse,
    Vector3 specular,
    float intensity) : IStd140Uniform
{
    public readonly Vector4 Direction = direction.AsVector4();
    public readonly Vector4 Diffuse = diffuse.AsVector4();
    public readonly Vector4 SpecularIntensity = new(specular, intensity);

    public Vector3 Specular => SpecularIntensity.AsVector3();
    public float Intensity => SpecularIntensity.W;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct ShadowUniformRecord(
    in Matrix4x4 lightViewProj,
    Vector4 shadowParams0,
    Vector4 shadowParams1) : IStd140Uniform
{
    public readonly Matrix4x4 LightViewProj = lightViewProj;
    public readonly Vector4 ShadowParams0 = shadowParams0;
    public readonly Vector4 ShadowParams1 = shadowParams1;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct MaterialUniformRecord(
    Vector3 color,
    float shininess,
    float specularStrength,
    float uvRepeat
) : IStd140Uniform
{
    public readonly Vector3 Color = color;
    public readonly float Shininess = shininess;
    public readonly float SpecularStrength = specularStrength;
    public readonly float UvRepeat = uvRepeat;
    private readonly Vector2 _pad0 = default;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawObjectUniform(
    in Matrix4x4 model,
    in Matrix3 normal
) : IStd140Uniform
{
    public readonly Matrix4x4 Model = model;

    public readonly Vector4 NormalCol0 = new(
        normal.M11, normal.M21, normal.M31, 0f);

    public readonly Vector4 NormalCol1 = new(
        normal.M12, normal.M22, normal.M32, 0f);

    public readonly Vector4 NormalCol2 = new(
        normal.M13, normal.M23, normal.M33, 0f);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FramePostProcessUniform(
    Vector4 colorAdjust,
    Vector4 whiteBalance,
    Vector4 flags,
    Vector4 bloomParams,
    Vector4 bloomLods,
    Vector4 lutParams,
    Vector4 vignetteParams,
    Vector4 grainParams,
    Vector4 chromAbParams,
    Vector4 toneShadows,
    Vector4 toneHighlights,
    Vector4 sharpenParams
) : IStd140Uniform
{
    public readonly Vector4 ColorAdjust = colorAdjust;
    public readonly Vector4 WhiteBalance = whiteBalance;
    public readonly Vector4 Flags = flags;
    public readonly Vector4 BloomParams = bloomParams;
    public readonly Vector4 BloomLods = bloomLods;
    public readonly Vector4 LutParams = lutParams;
    public readonly Vector4 VignetteParams = vignetteParams;
    public readonly Vector4 GrainParams = grainParams;
    public readonly Vector4 ChromAbParams = chromAbParams;
    public readonly Vector4 ToneShadows = toneShadows;
    public readonly Vector4 ToneHighlights = toneHighlights;
    public readonly Vector4 SharpenParams = sharpenParams;
}