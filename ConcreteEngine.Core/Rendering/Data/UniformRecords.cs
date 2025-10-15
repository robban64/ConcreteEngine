#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Registry;
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
public readonly struct EngineUniformRecord(
    float time,
    float deltaTime,
    float random,
    Vector2 invResolution,
    Vector2 mouse
) : IStd140Uniform
{
    // x = seconds since start, y = frame time step, z = per-frame random, w = pad
    public readonly Vector4 EngineParams0 = new(time, deltaTime, random, 0);

    // xy = 1.0 / screen resolution, zw = mouse
    public readonly Vector4 EngineParams1 = new(invResolution.X, invResolution.Y, mouse.X, mouse.Y);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FrameUniformRecord(
    Vector4 ambient,
    Vector4 ambientGround,
    Vector4 fogColor,
    Vector4 fogParams0,
    Vector4 fogParams1
) : IStd140Uniform
{
    public readonly Vector4 Ambient = ambient; // xyz = sky ambient, w = exposure
    public readonly Vector4 AmbientGround = ambientGround; // xyz = ground ambient
    public readonly Vector4 FogColor = fogColor; // rgb = base fog color, a = in-scattering mix
    public readonly Vector4 FogParams0 = fogParams0; // x=exp2_k, y=height_k, z=height0, w=globalStrength
    public readonly Vector4 FogParams1 = fogParams1; // x=expWeight, y=heightWeight, z=maxDistance, w=reserved
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
    Vector4 direction,
    Vector4 diffuse,
    Vector4 specular) : IStd140Uniform
{
    public readonly Vector4 Direction = direction; // direction, light toward scene
    public readonly Vector4 Diffuse = diffuse; // rgb=color, a=intensity
    public readonly Vector4 SpecularIntensity = specular; // x = specular multiplier
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct LightUniformRecord(
    int lightCounts,
    LightDataStruct l0,
    LightDataStruct l1 = default,
    LightDataStruct l2 = default,
    LightDataStruct l3 = default,
    LightDataStruct l4 = default,
    LightDataStruct l5 = default,
    LightDataStruct l6 = default,
    LightDataStruct l7 = default) : IStd140Uniform
{
    // yzw unused/padding
    public readonly IVec4Std140 LightCounts = new(lightCounts);

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
public readonly struct ShadowUniformRecord(
    in Matrix4x4 lightViewProj,
    Vector4 shadowParams0,
    Vector4 shadowParams1) : IStd140Uniform
{
    public readonly Matrix4x4 LightViewProj = lightViewProj;
    public readonly Vector4 ShadowParams0 = shadowParams0; // x=1/texW, y=1/texH, z=constBias, w=slopeBias
    public readonly Vector4 ShadowParams1 = shadowParams1; // x=strength, y=pcfRadius, z=NormalBias,w reserved
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct MaterialUniformRecord : IStd140Uniform
{
    public readonly Vector4 MatColor; // rgb = tint
    public readonly Vector4 MatParams0; // x = SpecularStrength, y = uvRepeat, z,w reserved
    public readonly Vector4 MatParams1; // x = Shininess, yzw reserved

    public MaterialUniformRecord(Vector4 matColor,
        Vector4 matParams0,
        Vector4 matParams1)
    {
        MatColor = matColor;
        MatParams0 = matParams0;
        MatParams1 = matParams1;
    }

    public MaterialUniformRecord(in MaterialParams mat)
    {
        MatColor = new Vector4(mat.Color.AsVec3(), 1);
        MatParams0 = new Vector4(mat.Specular, mat.UvRepeat, 0.0f, 0.0f);
        MatParams0 = new Vector4(mat.Shininess, mat.Normal, 0.0f, 0.0f);
    }
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
public readonly struct PostProcessUniform(
    Vector4 grade,
    Vector4 whiteBalance,
    Vector4 bloom,
    Vector4 fx
) : IStd140Uniform
{
    // x = exposureOffset (-0.10..+0.10), y = saturation (0.8..1.2)
    // z = contrast (0.9..1.1),w = warmth (-0.05..+0.05)
    public readonly Vector4 Grade = grade;

    //x = tint (-0.05..+0.05), y = strength (0..1), z,w = 0
    public readonly Vector4 WhiteBalance = whiteBalance;

    //x = intensity (0..1.5), y = threshold (0.6..0.9), z = radius (px), w = 0
    public readonly Vector4 Bloom = bloom;

    // x = vignetteStrength (0..0.15), y = grainAmount (0..0.01), z = sharpenAmount (0..0.15), w = rolloff (0..0.12)
    public readonly Vector4 Fx = fx;
}