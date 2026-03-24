using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer.Visuals;

namespace ConcreteEngine.Core.Renderer;

public sealed class VisualEnvironment
{
    public readonly ref struct MutateScope(VisualEnvironment env)
    {
        public ref AmbientParams Ambient => ref env._ambient;
        public ref FogParams Fog => ref env._fog;
        public ref SunLightParams SunLight => ref env._sunLight;
        public ref ShadowParams Shadow => ref env._shadow;
        public ref PostEffectParams PostEffect => ref env._postEffect;
    }

    public ulong Version { get; private set; }
    public bool Dirty { get; private set; }
    public bool WasDirty { get; private set; }
    public Size2D ScreenFboSize { get; private set; }

    private ShadowParams _shadow;
    private SunLightParams _sunLight = VisualUtils.MakeDefaultSunLight();
    private AmbientParams _ambient = VisualUtils.MakeDefaultAmbient();
    private FogParams _fog = VisualUtils.MakeDefaultFog();
    private PostEffectParams _postEffect = VisualUtils.MakeDefaultPostEffect();


    internal VisualEnvironment(Size2D screenFboSize, int shadowSize)
    {
        Dirty = true;
        WasDirty = false;
        ScreenFboSize = screenFboSize;
        _shadow = VisualUtils.MakeSizedShadow(shadowSize);
    }

    public MutateScope Mutate()
    {
        Dirty = true;
        return new MutateScope(this);
    }

    public ref readonly AmbientParams GetAmbient() => ref _ambient;
    public ref readonly SunLightParams GetDirectionalLight() => ref _sunLight;
    public ref readonly ShadowParams GetShadow() => ref _shadow;
    public ref readonly FogParams GetFog() => ref _fog;
    public ref readonly PostEffectParams GetPostEffect() => ref _postEffect;

    internal void SetScreenFboSize(Size2D outputSize)
    {
        ScreenFboSize = outputSize;
        Dirty = true;
    }

    public void SetDirectionalLight(in SunLightParams param)
    {
        _sunLight = param;
        Dirty = true;
    }

    public void SetAmbient(in AmbientParams param)
    {
        _ambient = param;
        Dirty = true;
    }

    public void SetFog(in FogParams param)
    {
        _fog = param;
        Dirty = true;
    }

    public void SetPostEffect(in PostEffectParams param)
    {
        _postEffect = param;
        Dirty = true;
    }

    public void SetShadow(in ShadowParams param)
    {
        _shadow = param;
        Dirty = true;
    }

    public bool SetShadowSize(int size)
    {
        ArgumentOutOfRangeException.ThrowIfZero(IntMath.IsPowerOfTwo(size) ? 1 : 0, nameof(size));
        if (size == _shadow.ShadowMapSize) return false;

        _shadow = VisualUtils.MakeSizedShadow(size);
        Dirty = true;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Ensure()
    {
        if (Dirty && !WasDirty)
        {
            Dirty = false;
            WasDirty = true;
            Version++;
        }
    }
}

file static class VisualUtils
{
    public static SunLightParams MakeDefaultSunLight() =>
        new(
            direction: new Vector3(-0.35f, -0.95f, 0.25f),
            diffuse: new Vector3(1.05f, 0.92f, 0.82f),
            intensity: 1.35f,
            specular: 0.75f
        );

    public static AmbientParams MakeDefaultAmbient() =>
        new(
            ambient: new Vector3(0.34f, 0.38f, 0.44f),
            ambientGround: new Vector3(0.20f, 0.17f, 0.15f),
            exposure: 0.26f
        );

    public static FogParams MakeDefaultFog() =>
        new(
            color: new Vector3(0.70f, 0.89f, 0.68f),
            density: 720f,
            heightFalloff: 5200f,
            baseHeight: 0f,
            strength: 1.05f,
            heightInfluence: 0.85f,
            scattering: 0.09f,
            maxDistance: 9500f
        );

    public static PostEffectParams MakeDefaultPostEffect() =>
        new(
            grade: new PostGradeParams(1.0f, 1.1f, 1.05f, 0.0f),
            whiteBalance: new PostWhiteBalanceParams(0.0f, 0.0f),
            bloom: new PostBloomParams(0.5f, 0.85f, 3.0f),
            imageFx: new PostImageFxParams(0.25f, 0.15f, 0.20f, 0.0f)
        );


    public static ShadowParams MakeSizedShadow(int size)
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

        return new ShadowParams(
            shadowMapSize: size,
            distance: distance,
            zPad: 20.0f,
            constBias: constBias,
            slopeBias: slopeBias,
            strength: 1f,
            pcfRadius: 1f);
    }
}