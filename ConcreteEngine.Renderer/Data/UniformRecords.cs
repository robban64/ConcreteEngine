#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Renderer.Data;

public abstract class LightUboTag;

public abstract class EngineUboTag;

public abstract class FrameUboTag;

public abstract class CameraUboTag;

public abstract class DirLightUboTag;

public abstract class ShadowUboTag;

public abstract class MaterialUboTag;

public abstract class DrawUboTag;

public abstract class PostUboTag;

[StructLayout(LayoutKind.Sequential)]
public readonly struct LightDataStruct(
    in Vector4 colorIntensity,
    in Vector4 posRange,
    in Vector4 dirType,
    in Vector4 spotAngles
) : IStd140Uniform
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
)
{
    // x = seconds since start, y = frame time step, z = per-frame random, w = pad
    public readonly Vector4 EngineParams0 = new(time, deltaTime, random, 0);

    // xy = 1.0 / screen resolution, zw = mouse
    public readonly Vector4 EngineParams1 = new(invResolution.X, invResolution.Y, mouse.X, mouse.Y);
}

[StructLayout(LayoutKind.Sequential)]
public struct FrameUniformRecord(
    in Vector4 ambient,
    in Vector4 ambientGround,
    in Vector4 fogColor,
    in Vector4 fogParams0,
    in Vector4 fogParams1
)
{
    public Vector4 Ambient = ambient; // xyz = sky ambient, w = exposure
    public Vector4 AmbientGround = ambientGround; // xyz = ground ambient
    public Vector4 FogColor = fogColor; // rgb = base fog color, a = in-scattering mix
    public Vector4 FogParams0 = fogParams0; // x=exp2_k, y=height_k, z=height0, w=globalStrength
    public Vector4 FogParams1 = fogParams1; // x=expWeight, y=heightWeight, z=maxDistance, w=reserved
}

[StructLayout(LayoutKind.Sequential)]
public struct CameraUniformRecord(
    in Matrix4x4 viewMat,
    in Matrix4x4 projMat,
    in Matrix4x4 projViewMat,
    in Vector4 cameraPos,
    in Vector4 cameraUp,
    in Vector4 cameraRight)
{
    public Matrix4x4 ViewMat = viewMat;
    public Matrix4x4 ProjMat = projMat;
    public Matrix4x4 ProjViewMat = projViewMat;
    public Vector4 CameraPos = cameraPos;
    public Vector4 CameraUp = cameraUp;
    public Vector4 CameraRight = cameraRight;
}

[StructLayout(LayoutKind.Sequential)]
public struct DirLightUniformRecord(
    in Vector4 direction,
    in Vector4 diffuse,
    in Vector4 specular)
{
    public Vector4 Direction = direction; // direction, light toward scene
    public Vector4 Diffuse = diffuse; // rgb=color, a=intensity
    public Vector4 Specular = specular; // x = specular multiplier
}

[StructLayout(LayoutKind.Sequential)]
public struct LightUniformRecord(
    int lightCounts
    /*LightDataStruct l0,
    LightDataStruct l1 = default,
    LightDataStruct l2 = default,
    LightDataStruct l3 = default,
    LightDataStruct l4 = default,
    LightDataStruct l5 = default,
    LightDataStruct l6 = default,
    LightDataStruct l7 = default*/)
{
    // yzw unused/padding
    public IVec4Std140 LightCounts = new(lightCounts);
/*
    public LightDataStruct L0 = l0;
    public LightDataStruct L1 = l1;
    public LightDataStruct L2 = l2;
    public LightDataStruct L3 = l3;
    public LightDataStruct L4 = l4;
    public LightDataStruct L5 = l5;
    public LightDataStruct L6 = l6;
    public LightDataStruct L7 = l7;
*/
}

[StructLayout(LayoutKind.Sequential)]
public struct ShadowUniformRecord(
    in Matrix4x4 lightViewProj,
    Vector4 shadowParams0,
    Vector4 shadowParams1)
{
    public Matrix4x4 LightViewProj = lightViewProj;
    public Vector4 ShadowParams0 = shadowParams0; // x=1/texW, y=1/texH, z=constBias, w=slopeBias
    public Vector4 ShadowParams1 = shadowParams1; // x=strength, y=pcfRadius, z=NormalBias,w reserved
}

[StructLayout(LayoutKind.Sequential)]
public struct MaterialUniformRecord
{
    public Vector4 MatColor; // rgb = tint
    public Vector4 MatParams0; // x = SpecularStrength, y = uvRepeat, z,w reserved
    public Vector4 MatParams1; // x = Shininess, y = HasNormals z = Transparency, w = HasAlpha

    public MaterialUniformRecord(
        in Vector4 matColor,
        in Vector4 matParams0,
        in Vector4 matParams1)
    {
        MatColor = matColor;
        MatParams0 = matParams0;
        MatParams1 = matParams1;
    }

    public MaterialUniformRecord(in MaterialParams mat)
    {
        mat.Fill(out var color, out var param1, out var param2);
        MatColor = color;
        MatParams0 = param1;
        MatParams1 = param2;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawObjectUniform
{
    public Matrix4x4 Model;
    public Matrix3X4 Normal;
    
    public DrawObjectUniform()
    {
    }

    public DrawObjectUniform(in Matrix4x4 model, in Vector4 v0, in Vector4 v1, in Vector4 v2)
    {
        Model = model;
        Normal.V0 = v0;
        Normal.V1 = v1;
        Normal.V2 = v2;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct PostProcessUniform(
    Vector4 grade,
    Vector4 whiteBalance,
    Vector4 bloom,
    Vector4 fx
)
{
    // x = exposureOffset (-0.10..+0.10), y = saturation (0.8..1.2)
    // z = contrast (0.9..1.1),w = warmth (-0.05..+0.05)
    public Vector4 Grade = grade;

    //x = tint (-0.05..+0.05), y = strength (0..1), z,w = 0
    public Vector4 WhiteBalance = whiteBalance;

    //x = intensity (0..1.5), y = threshold (0.6..0.9), z = radius (px), w = 0
    public Vector4 Bloom = bloom;

    // x = vignetteStrength (0..0.15), y = grainAmount (0..0.01), z = sharpenAmount (0..0.15), w = rolloff (0..0.12)
    public Vector4 Fx = fx;
}