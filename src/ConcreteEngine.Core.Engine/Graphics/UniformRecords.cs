using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct LightDataStruct(
    in Vector4 colorIntensity,
    in Vector4 posRange,
    in Vector4 dirType,
    in Vector4 spotAngles
)
{
    public Vector4 ColorIntensity = colorIntensity;
    public Vector4 PosRange = posRange;
    public Vector4 DirType = dirType;
    public Vector4 SpotAngles = spotAngles;
}

[StructLayout(LayoutKind.Sequential)]
public struct EngineUniformRecord : IUniform
{
    // x = seconds since start, y = frame time step, z = per-frame random, w = pad
    public Vector4 EngineParams0;

    // xy = 1.0 / screen resolution, zw = mouse
    public Vector4 EngineParams1;

    public EngineUniformRecord(float time,
        float deltaTime,
        float random,
        Vector2 invResolution,
        Vector2 mouse)
    {
        EngineParams0 = new Vector4(time, deltaTime, random, 0);
        EngineParams1 = new Vector4(invResolution.X, invResolution.Y, mouse.X, mouse.Y);
    }
    
    public static UniformBufferId UboId { get; set; }

    public static byte Slot => 0;
    public static int Stride => Unsafe.SizeOf<EngineUniformRecord>();
}

[StructLayout(LayoutKind.Sequential)]
public struct FrameUniform : IUniform
{
    public Vector4 Ambient; // xyz = sky ambient, w = exposure
    public Vector4 AmbientGround; // xyz = ground ambient
    public Vector4 FogColor; // rgb = base fog color, a = in-scattering mix
    public Vector4 FogParams0; // x=exp2_k, y=height_k, z=height0, w=globalStrength
    public Vector4 FogParams1; // x=expWeight, y=heightWeight, z=maxDistance, w=reserved

    
    public static UniformBufferId UboId { get; set; }

    public static byte Slot => 1;
    public static int Stride => Unsafe.SizeOf<FrameUniform>();
}

[StructLayout(LayoutKind.Sequential)]
public struct CameraUniform : IUniform
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

    
    public static UniformBufferId UboId { get; set; }

    public static byte Slot => 2;
    public static int Stride => Unsafe.SizeOf<CameraUniform>();
}

[StructLayout(LayoutKind.Sequential)]
public struct DirectionalLightUniform : IUniform
{
    public Vector4 Direction; // direction, light toward scene
    public Vector4 Diffuse; // rgb=color, a=intensity
    public Vector4 Specular; // x = specular multiplier

    
    public static UniformBufferId UboId { get; set; }

    public static byte Slot => 3;
    public static int Stride => Unsafe.SizeOf<DirectionalLightUniform>();
}

[StructLayout(LayoutKind.Sequential)]
public struct LightUniform : IUniform
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

    
    public static UniformBufferId UboId { get; set; }

    public static byte Slot => 4;
    public static int Stride => Unsafe.SizeOf<LightUniform>();

}

[StructLayout(LayoutKind.Sequential)]
public struct ShadowUniform : IUniform
{
    public Matrix4x4 LightViewProj;
    public Vector4 ShadowParams0; // x=1/texW, y=1/texH, z=constBias, w=slopeBias
    public Vector4 ShadowParams1; // x=strength, y=pcfRadius, z=NormalBias, w=MaxDistance

    
    public static UniformBufferId UboId { get; set; }

    public static byte Slot => 5;
    public static int Stride => Unsafe.SizeOf<ShadowUniform>();
}

[StructLayout(LayoutKind.Sequential)]
public struct MaterialUniform : IUniform
{
    public Color4 Color;
    public Color4 SpecularColor;
    public Vector4 UvTransform;

    public float Shininess;
    public float Roughness;
    public float Metallic;
    public float AlphaCutoff;

    public int AlphaMaskToggle;
    public int ShadowToggle;
    private int _pad22, _pad23;

    
    public static UniformBufferId UboId { get; set; }

    public static byte Slot => 6;
    public static int Stride => Unsafe.SizeOf<MaterialUniform>();
}

[StructLayout(LayoutKind.Sequential)]
public struct TransformUniform : IUniform
{
    public Matrix4x4 Model;
    public Matrix3X4 Normal;
    
    public static UniformBufferId UboId { get; set; }

    public static byte Slot => 7;
    public static int Stride => Unsafe.SizeOf<TransformUniform>();
}

[StructLayout(LayoutKind.Sequential)]
public struct SkinningUniform : IUniform
{
    public Matrix4x4 BoneMatrix;
    
    public static UniformBufferId UboId { get; set; }

    public static byte Slot => 8;
    public static int Stride => 64;
}

[StructLayout(LayoutKind.Sequential)]
public struct PostFxUniform : IUniform
{
    public Vector4 Grade;
    public Vector4 WhiteBalance;
    public Vector4 Bloom;
    public Vector4 Fx;
    
    
    public static UniformBufferId UboId { get; set; }
    public static byte Slot => 9;
    public static int Stride => Unsafe.SizeOf<PostFxUniform>();
}

[StructLayout(LayoutKind.Sequential)]
public struct EditorEffectsUniform : IUniform
{
    public int IsAnimated;
    private int _effectPad1, _effectPad2, _effectPad3;
    public Vector4 EffectColor1;

    
    public static UniformBufferId UboId { get; set; }
    public static byte Slot => 10;
    public static int Stride => Unsafe.SizeOf<EditorEffectsUniform>();

    public EditorEffectsUniform(bool isAnimated, in Color4 effectColor1)
    {
        IsAnimated = isAnimated ? 1 : 0;
        EffectColor1 = effectColor1;
    }
}