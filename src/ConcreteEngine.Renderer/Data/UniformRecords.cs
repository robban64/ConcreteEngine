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
public struct FrameUniform
{
    public Vector4 Ambient; // xyz = sky ambient, w = exposure
    public Vector4 AmbientGround; // xyz = ground ambient
    public Vector4 FogColor; // rgb = base fog color, a = in-scattering mix
    public Vector4 FogParams0; // x=exp2_k, y=height_k, z=height0, w=globalStrength
    public Vector4 FogParams1; // x=expWeight, y=heightWeight, z=maxDistance, w=reserved
}

[StructLayout(LayoutKind.Sequential)]
public struct CameraUniform
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

    public CameraUniform(Vector3 translation, in CameraMatrices data)
    {
        ViewMat = data.ViewMatrix;
        ProjMat = data.ProjectionMatrix;
        ProjViewMat = data.ProjectionViewMatrix;
        CameraPos = translation;
        CameraUp = data.Up;
        CameraRight = data.Right;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct DirectionalLightUniform
{
    public Vector4 Direction; // direction, light toward scene
    public Vector4 Diffuse; // rgb=color, a=intensity
    public Vector4 Specular; // x = specular multiplier
}

[StructLayout(LayoutKind.Sequential)]
public struct LightUniform
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
public struct ShadowUniform
{
    public Matrix4x4 LightViewProj;
    public Vector4 ShadowParams0; // x=1/texW, y=1/texH, z=constBias, w=slopeBias
    public Vector4 ShadowParams1; // x=strength, y=pcfRadius, z=NormalBias, w=MaxDistance
}

[StructLayout(LayoutKind.Sequential)]
public struct MaterialUniform
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
    public Vector4 Grade;
    public Vector4 WhiteBalance;
    public Vector4 Bloom;
    public Vector4 Fx;
}

[StructLayout(LayoutKind.Sequential)]
public struct EditorEffectsUniform(bool isAnimated, in Color4 effectColor1)
{
    public int IsAnimated = isAnimated ? 1 : 0;
    private int _effectPad1, _effectPad2, _effectPad3;
    public Vector4 EffectColor1 = effectColor1;
}