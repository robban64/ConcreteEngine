using System.Numerics;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds.Utility;

internal static class WorldParamUtils
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
        ArgumentOutOfRangeException.ThrowIfLessThan(size, RenderLimits.MinShadowMapSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RenderLimits.MaxShadowMapSize);

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