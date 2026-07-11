using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class VisualManager
{
    public static readonly VisualManager Instance = new();

    public bool AnyWasDirty { get; private set; }

    public readonly ShadowSettings Shadow;
    public readonly IlluminationSettings Illumination;
    public readonly EnvironmentSettings Environment;
    public readonly PostEffectSettings PostEffect;

    public bool HasPendingShadowSize => Shadow.HasPendingShadowSize;

    private VisualManager()
    {
        if (Instance != null)
            throw new InvalidOperationException($"{nameof(VisualManager)} is already initialized");

        Shadow = new ShadowSettings();
        Illumination = new IlluminationSettings();
        Environment = new EnvironmentSettings();
        PostEffect = new PostEffectSettings();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CommitShadowSize()
    {
        var hasPendingShadowSize = Shadow.HasPendingShadowSize;
        if (hasPendingShadowSize) Shadow.HasPendingShadowSize = false;
        return hasPendingShadowSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Commit()
    {
        AnyWasDirty = false;
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

public abstract class VisualStateObject
{
    public ulong Version { get; private set; }
    public bool WasDirty { get; private set; }

    protected bool IsDirty = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Ensure()
    {
        if (!IsDirty && WasDirty)
        {
            WasDirty = false;
        }
        else if (IsDirty && !WasDirty)
        {
            IsDirty = false;
            WasDirty = true;
            Version++;
        }

        return WasDirty;
    }
}

[Inspect]
public sealed class IlluminationSettings : VisualStateObject
{
    [InspectInclude]
    public DirLightParams DirectionalLight
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(new Vector3(-0.35f, -0.95f, 0.25f), new Vector3(1.05f, 0.92f, 0.82f), 1.35f, 0.75f);

    [InspectInclude]
    public AmbientParams Ambient  
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(new Vector3(0.34f, 0.38f, 0.44f), new Vector3(0.20f, 0.17f, 0.15f), 0.26f);
}

[Inspect]
public sealed class EnvironmentSettings : VisualStateObject
{
    [InputColor]
    public Color4 FogColor
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(0.70f, 0.89f, 0.68f);

    [InspectInclude]
    public FogHeightParams FogHeight
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(720f, 1.05f, 9500f, 0, 5200f);
    
    [InspectInclude]
    public FogOpticsParams FogOptics
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(0.09f, 1f, 0.85f);


}

[Inspect]
public sealed class ShadowSettings : VisualStateObject
{
    public bool HasPendingShadowSize { get; internal set; }


    [InputCombo("ShadowMap Size", [1024, 2048, 4096, 8192], ["1024px", "2048px", "4096px", "8192px"])]
    public int ShadowMapSize
    {
        get;
        set
        {
            if (field == value) return;
            ArgumentOutOfRangeException.ThrowIfEqual(IntMath.IsPowerOfTwo(value), false, nameof(value));

            field = value;
            Projection = VisualUtils.MakeSizedShadow(value, 20.0f);
            IsDirty = true;
            HasPendingShadowSize = true;
        }
    }

    [InspectInclude]
    public ShadowProjectionParams Projection
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }
    
    [InspectInclude]
    public ShadowVisualParams Visuals
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }= new(1f, 1f);
}

[Inspect]
public sealed class PostEffectSettings : VisualStateObject
{
    [InspectInclude]
    public PostGradeParams Grade
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(1.0f, 1.1f, 1.05f, 0.0f);

    [InspectInclude]
    public PostWhiteBalanceParams WhiteBalance
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(0.0f, 0.0f);
    
    [InspectInclude]
    public PostBloomParams Bloom
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(0.5f, 0.85f, 3.0f);
    
    public PostImageFxParams ImageFx
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = new(0.25f, 0.15f, 0.20f, 0.0f);
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