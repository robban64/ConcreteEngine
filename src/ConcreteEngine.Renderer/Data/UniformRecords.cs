using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Primitives;
using ConcreteEngine.Core.Renderer.Data;

namespace ConcreteEngine.Renderer.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly struct LightDataStruct(
    in Vector4 colorIntensity,
    in Vector4 posRange,
    in Vector4 dirType,
    in Vector4 spotAngles
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
)
{
    // x = seconds since start, y = frame time step, z = per-frame random, w = pad
    public readonly Vector4 EngineParams0 = new(time, deltaTime, random, 0);

    // xy = 1.0 / screen resolution, zw = mouse
    public readonly Vector4 EngineParams1 = new(invResolution.X, invResolution.Y, mouse.X, mouse.Y);
}

[StructLayout(LayoutKind.Sequential)]
public struct FrameUniformRecord
{
    public Vector4 Ambient; // xyz = sky ambient, w = exposure
    public Vector4 AmbientGround; // xyz = ground ambient
    public Vector4 FogColor; // rgb = base fog color, a = in-scattering mix
    public Vector4 FogParams0; // x=exp2_k, y=height_k, z=height0, w=globalStrength
    public Vector4 FogParams1; // x=expWeight, y=heightWeight, z=maxDistance, w=reserved
}

[StructLayout(LayoutKind.Sequential)]
public struct CameraUniformRecord
{
    public Matrix4x4 ViewMat;
    public Matrix4x4 ProjMat;
    public Matrix4x4 ProjViewMat;

    public Vector3 CameraPos;
    private float _cameraPosPad;

    public Vector3 CameraUp;
    private float _cameraUpPad;

    public Vector3 CameraRight;
    private float _cameraRightPad;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillView(in CameraMatrices data)
    {
        ViewMat = data.ViewMatrix;
        ProjMat = data.ProjectionMatrix;
        ProjViewMat = data.ProjectionViewMatrix;
        CameraUp = data.Up;
        CameraRight = data.Right;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct DirLightUniformRecord
{
    public Vector4 Direction; // direction, light toward scene
    public Vector4 Diffuse; // rgb=color, a=intensity
    public Vector4 Specular; // x = specular multiplier
}

[StructLayout(LayoutKind.Sequential)]
public struct LightUniformRecord
{
    // yzw unused/padding
    public Int4 LightCounts;

    public LightDataStruct L0;
    public LightDataStruct L1;
    public LightDataStruct L2;
    public LightDataStruct L3;
    public LightDataStruct L4;
    public LightDataStruct L5;
    public LightDataStruct L6;
    public LightDataStruct L7;
}

[StructLayout(LayoutKind.Sequential)]
public struct ShadowUniformRecord
{
    public Matrix4x4 LightViewProj;
    public Vector4 ShadowParams0; // x=1/texW, y=1/texH, z=constBias, w=slopeBias
    public Vector4 ShadowParams1; // x=strength, y=pcfRadius, z=NormalBias, w=MaxDistance
}

[StructLayout(LayoutKind.Sequential)]
public struct MaterialUniformRecord
{
    public Vector4 MatColor; // rgb = tint
    public Vector4 MatParams0; // x = SpecularStrength, y = uvRepeat, z,w reserved
    public Vector4 MatParams1; // x = Shininess, y = HasNormals z = Transparency, w = HasAlpha
}

[StructLayout(LayoutKind.Sequential)]
public struct DrawObjectUniform
{
    public Matrix4x4 Model;
    public Matrix3X4 Normal;
    private Vector4 _pad;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct DrawAnimationUniform
{
    public const int MaxBones = 64;
    public const int Mat4Components = 16;
    public const int TotalComponents = Mat4Components * MaxBones;

    public fixed float Weights[TotalComponents];
}

[StructLayout(LayoutKind.Sequential)]
public struct PostProcessUniform
{
    // x = exposureOffset (-0.10..+0.10), y = saturation (0.8..1.2)
    // z = contrast (0.9..1.1),w = warmth (-0.05..+0.05)
    public Vector4 Grade;

    //x = tint (-0.05..+0.05), y = strength (0..1), z,w = 0
    public Vector4 WhiteBalance;

    //x = intensity (0..1.5), y = threshold (0.6..0.9), z = radius (px), w = 0
    public Vector4 Bloom;

    // x = vignetteStrength (0..0.15), y = grainAmount (0..0.01), z = sharpenAmount (0..0.15), w = rolloff (0..0.12)
    public Vector4 Fx;
}

[StructLayout(LayoutKind.Sequential)]
public struct EditorEffectsUniform(bool isAnimated, in Color4 effectColor1)
{
    public int IsAnimated = isAnimated ? 1 : 0;
    private int _effectPad1, _effectPad2, _effectPad3;
    public Vector4 EffectColor1 = effectColor1;
}