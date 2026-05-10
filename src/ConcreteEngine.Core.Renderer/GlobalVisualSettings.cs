/*
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Visuals;

namespace ConcreteEngine.Core.Renderer;

public abstract class GlobalVisualSettingsEntry
{
    public bool IsDirty { get; protected set; } = true;
    public bool WasDirty { get; protected set; }
    public ulong Version { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Ensure()
    {
        if (IsDirty && !WasDirty)
        {
            IsDirty = false;
            WasDirty = true;
            Version++;
        }

        return WasDirty;
    }
}

public sealed class GlobalVisualSettings
{
    public static GlobalVisualSettings Instance { get; private set; } = null!;

    internal static void Make(int shadowSize)
    {
        if (Instance is not null)
            throw new InvalidOperationException("Global Visual Settings has already been initialized");
        Instance = new GlobalVisualSettings(shadowSize);
    }

    public bool AnyWasDirty { get; private set; }

    public readonly IlluminationSettings Illumination;
    public readonly EnvironmentSettings Environment;
    public readonly ShadowSettings Shadow;
    public readonly PostEffectSettings PostEffect;

    private GlobalVisualSettings(int shadowSize)
    {
        Illumination = new IlluminationSettings();
        Environment = new EnvironmentSettings();
        PostEffect = new PostEffectSettings();
        Shadow = new ShadowSettings(shadowSize);
    }


    public void Ensure()
    {
        AnyWasDirty = false;
        AnyWasDirty |= PostEffect.Ensure();
        AnyWasDirty |= Environment.Ensure();
        AnyWasDirty |= Illumination.Ensure();
        AnyWasDirty |= Shadow.Ensure();
    }
}

public sealed class IlluminationSettings : GlobalVisualSettingsEntry
{
    public Vector3 Direction
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public Color4 Diffuse
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float Intensity
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float Specular
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public Color4 Ambient
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public Color4 AmbientGround
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float Exposure
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }
}

public sealed class EnvironmentSettings : GlobalVisualSettingsEntry
{
    public Color4 FogColor
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }


    public FogHeightParams FogHeight
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public FogOpticsParams FogOptics
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }
}

public sealed class ShadowSettings(int shadowSize) : GlobalVisualSettingsEntry
{
    public int ShadowMapSize
    {
        get;
        internal set
        {
            field = value;
            IsDirty = true;
        }
    } = shadowSize;

    public ShadowProjectionParams Projection
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public ShadowVisualParams Visuals
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }


    public float Distance
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float ZPad
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float ConstBias
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float SlopeBias
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float Strength
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float PcfRadius
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }
}

public sealed class PostEffectSettings : GlobalVisualSettingsEntry
{
    private PostGradeParams _grade;
    private PostWhiteBalanceParams _whiteBalance;
    private PostBloomParams _bloom;
    private PostImageFxParams _imageFx;

    public PostGradeParams Grade
    {
        get => _grade;
        set
        {
            _grade = value;
            IsDirty = true;
        }
    }

    public PostWhiteBalanceParams WhiteBalance
    {
        get;
        set
        {
            _whiteBalance = value;
            IsDirty = true;
        }
    }

    public PostBloomParams Bloom
    {
        get;
        set
        {
            _bloom = value;
            IsDirty = true;
        }
    }

    public PostImageFxParams ImageFx
    {
        get;
        set
        {
            _imageFx = value;
            IsDirty = true;
        }
    }
}
*/