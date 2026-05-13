using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer.Visuals;

namespace ConcreteEngine.Core.Renderer;

public abstract class VisualStateObject
{
    public ulong Version { get; private set; }
    public bool IsDirty => _isDirty;
    public bool WasDirty { get; private set; }

    protected bool _isDirty = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Ensure()
    {
        if (IsDirty && !WasDirty)
        {
            _isDirty = false;
            WasDirty = true;
            Version++;
        }

        return WasDirty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected StateScope<T> MakeScope<T>(ref T value) where T : unmanaged => new(ref value, ref _isDirty);
}

public sealed class GlobalVisualSettings
{
    internal static readonly GlobalVisualSettings Instance = new();

    public bool AnyWasDirty { get; private set; }
    public bool HasPendingOutputSize { get; private set; }

    public readonly ShadowSettings Shadow;
    public readonly IlluminationSettings Illumination;
    public readonly EnvironmentSettings Environment;
    public readonly PostEffectSettings PostEffect;

    public bool HasPendingFrameBufferResize => HasPendingOutputSize || Shadow.HasPendingShadowSize;

    internal GlobalVisualSettings()
    {
        if (Instance != null)
            throw new InvalidOperationException($"{nameof(GlobalVisualSettings)} is already initialized");

        Shadow = new ShadowSettings();
        Illumination = new IlluminationSettings();
        Environment = new EnvironmentSettings();
        PostEffect = new PostEffectSettings();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MarkPendingOutputSize() => HasPendingOutputSize = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ClearDirty()
    {
        AnyWasDirty = false;
        HasPendingOutputSize = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Ensure()
    {
        AnyWasDirty = false;
        AnyWasDirty |= Illumination.Ensure();
        AnyWasDirty |= Shadow.Ensure();
        AnyWasDirty |= Environment.Ensure();
        AnyWasDirty |= PostEffect.Ensure();
        return AnyWasDirty;
    }
}

public readonly ref struct StateScope<T>(ref T value, ref bool isDirty) where T : unmanaged
{
    private readonly ref T _value = ref value;
    private readonly ref bool _isDirty = ref isDirty;

    public ref readonly T Value => ref _value;

    public ref T Mutate
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            _isDirty = true;
            return ref _value;
        }
    }
}

public sealed class IlluminationSettings : VisualStateObject
{
    private SunLightParams _directionalLight = new(
        direction: new Vector3(-0.35f, -0.95f, 0.25f),
        diffuse: new Vector3(1.05f, 0.92f, 0.82f),
        intensity: 1.35f,
        specular: 0.75f
    );

    private AmbientParams _ambient = new(
        ambient: new Color4(0.34f, 0.38f, 0.44f),
        ambientGround: new Color4(0.20f, 0.17f, 0.15f),
        exposure: 0.26f
    );

    public StateScope<SunLightParams> DirectionalLight => MakeScope(ref _directionalLight);
    public StateScope<AmbientParams> Ambient => MakeScope(ref _ambient);
}

public sealed class EnvironmentSettings : VisualStateObject
{
    private FogHeightParams _fogHeight = new()
    {
        Density = 720f,
        Strength = 1.05f,
        MaxDistance = 9500f,
        BaseHeight = 0,
        HeightFalloff = 5200f
    };

    private FogOpticsParams _fogOptics = new()
    {
        Color = new Color4(0.70f, 0.89f, 0.68f), Scattering = 0.09f, DistanceWeight = 1f, HeightWeight = 0.85f
    };

    public StateScope<FogHeightParams> FogHeight => MakeScope(ref _fogHeight);
    public StateScope<FogOpticsParams> FogOptics => MakeScope(ref _fogOptics);
}

public sealed class ShadowSettings : VisualStateObject
{
    public bool HasPendingShadowSize { get; private set; }

    public int ShadowMapSize
    {
        get;
        set
        {
            if (field == value) return;
            ArgumentOutOfRangeException.ThrowIfEqual(IntMath.IsPowerOfTwo(value), false, nameof(value));

            field = value;
            _projection = VisualUtils.MakeSizedShadow(value, 20.0f);
            _isDirty = true;
            HasPendingShadowSize = true;
        }
    }

    private ShadowProjectionParams _projection;
    private ShadowVisualParams _visuals = new(1f, 1f);

    public StateScope<ShadowProjectionParams> Projection => MakeScope(ref _projection);
    public StateScope<ShadowVisualParams> Visuals => MakeScope(ref _visuals);
}

public sealed class PostEffectSettings : VisualStateObject
{
    private PostGradeParams _grade = new(1.0f, 1.1f, 1.05f, 0.0f);
    private PostWhiteBalanceParams _whiteBalance = new(0.0f, 0.0f);
    private PostBloomParams _bloom = new(0.5f, 0.85f, 3.0f);
    private PostImageFxParams _imageFx = new(0.25f, 0.15f, 0.20f, 0.0f);

    public StateScope<PostGradeParams> Grade => MakeScope(ref _grade);
    public StateScope<PostWhiteBalanceParams> WhiteBalance => MakeScope(ref _whiteBalance);
    public StateScope<PostBloomParams> Bloom => MakeScope(ref _bloom);
    public StateScope<PostImageFxParams> ImageFx => MakeScope(ref _imageFx);
}

file static class VisualUtils
{
    public static ShadowProjectionParams MakeSizedShadow(int size, float zPad)
    {
        //ArgumentOutOfRangeException.ThrowIfLessThan(size, RenderLimits.MinShadowMapSize);
        //ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RenderLimits.MaxShadowMapSize);

        //constBias
        // 4k-map =  0.0003 to 0.0005
        // 2k-map = 0.0001 to 0.0002

        //slopeBias
        // 2k-map = 0.0025 - 0.0035
        // 4k-map = 0.0015f-0.0025f

        int distance;
        float constBias, slopeBias;
        switch (size)
        {
            case 1024:
                distance = 60;
                constBias = 0.00025f;
                slopeBias = 0.0035f;
                break;
            case 2048:
                distance = 80;
                constBias = 0.0002f;
                slopeBias = 0.003f;
                break;
            case 4096:
                distance = 120;
                constBias = 0.0004f;
                slopeBias = 0.002f;
                break;
            case 8192:
                distance = 140;
                constBias = 0.00045f;
                slopeBias = 0.0015f;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(size));
        }

        return new ShadowProjectionParams(distance: distance, zPad: zPad, constBias: constBias, slopeBias: slopeBias);
    }
}