using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Visuals;

namespace ConcreteEngine.Core.Renderer;

public abstract class VisualSettings
{
    public bool Dirty { get; protected set; }
    public ulong Version { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Ensure()
    {
        if (!Dirty) return;
        Dirty = false;
        Version++;
    }
}

public sealed class IlluminationSettings : VisualSettings
{
    public Vector3 Direction
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public Color4 Diffuse
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public float Intensity
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public float Specular
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public Color4 Ambient
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public Color4 AmbientGround
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public float Exposure
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }
}

public sealed class ShadowSettings : VisualSettings
{
    public int ShadowMapSize
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public float Distance
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public float ZPad
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public float ConstBias
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public float SlopeBias
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public float Strength
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }

    public float PcfRadius
    {
        get;
        set
        {
            field = value;
            Dirty = true;
        }
    }
}

public sealed class PostEffectSettings : VisualSettings
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
            Dirty = true;
        }
    }

    public PostWhiteBalanceParams WhiteBalance
    {
        get;
        set
        {
            _whiteBalance = value;
            Dirty = true;
        }
    }

    public PostBloomParams Bloom
    {
        get;
        set
        {
            _bloom = value;
            Dirty = true;
        }
    }

    public PostImageFxParams ImageFx
    {
        get;
        set
        {
            _imageFx = value;
            Dirty = true;
        }
    }
}