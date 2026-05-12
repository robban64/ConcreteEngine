using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
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
    internal static GlobalVisualSettings Instance { get; private set; }

    internal static GlobalVisualSettings Make(int shadowSize) => Instance = new GlobalVisualSettings(shadowSize);

    internal bool AnyWasDirty { get; private set; }

    public readonly ShadowSettings Shadow;
    public readonly IlluminationSettings Illumination;
    public readonly EnvironmentSettings Environment;
    public readonly PostEffectSettings PostEffect;

    internal GlobalVisualSettings(int shadowSize)
    {
        if (Instance != null)
            throw new InvalidOperationException($"{nameof(GlobalVisualSettings)} is already initialized");
        
        Shadow = new ShadowSettings(shadowSize);
        Illumination = new IlluminationSettings();
        Environment = new EnvironmentSettings();
        PostEffect = new PostEffectSettings();
    }


    public void Ensure()
    {
        AnyWasDirty = false;
        AnyWasDirty |= Illumination.Ensure();
        AnyWasDirty |= Shadow.Ensure();
        AnyWasDirty |= Environment.Ensure();
        AnyWasDirty |= PostEffect.Ensure();
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
    private SunLightParams _directionalLight;
    private AmbientParams _ambient;
    public StateScope<SunLightParams> DirectionalLight => MakeScope(ref _directionalLight);
    public StateScope<AmbientParams> Ambient => MakeScope(ref _ambient);
}

public sealed class EnvironmentSettings : VisualStateObject
{
    private FogHeightParams _fogHeight;
    private FogOpticsParams _fogOptics;

    public StateScope<FogHeightParams> FogHeight => MakeScope(ref _fogHeight);
    public StateScope<FogOpticsParams> FogOptics => MakeScope(ref _fogOptics);
}

public sealed class ShadowSettings(int shadowSize) : VisualStateObject
{
    private ShadowProjectionParams _projection;
    private ShadowVisualParams _visuals;

    public int ShadowMapSize
    {
        get;
        set
        {
            field = value;
            _isDirty = true;
        }
    } = shadowSize;

    public StateScope<ShadowProjectionParams> Projection => MakeScope(ref _projection);
    public StateScope<ShadowVisualParams> Visuals => MakeScope(ref _visuals);
}

public sealed class PostEffectSettings : VisualStateObject
{
    private PostGradeParams _grade;
    private PostWhiteBalanceParams _whiteBalance;
    private PostBloomParams _bloom;
    private PostImageFxParams _imageFx;

    public StateScope<PostGradeParams> Grade => MakeScope(ref _grade);
    public StateScope<PostWhiteBalanceParams> WhiteBalance => MakeScope(ref _whiteBalance);
    public StateScope<PostBloomParams> Bloom => MakeScope(ref _bloom);
    public StateScope<PostImageFxParams> Fx => MakeScope(ref _imageFx);
}