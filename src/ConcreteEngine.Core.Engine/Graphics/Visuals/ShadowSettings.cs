using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Visuals;
// @formatter:off


[Inspect]
public sealed class ShadowSettings : VisualStateObject
{
    public float InvMapSize { get; private set; }
    public bool HasPendingShadowSize { get; internal set; }

    [InputCombo([1024, 2048, 4096, 8192], ["1024px", "2048px", "4096px", "8192px"], Label = "ShadowMap Size")]
    public int ShadowMapSize
    {
        get;
        set
        {
            if (field == value) return;
            ArgumentOutOfRangeException.ThrowIfEqual(IntMath.IsPowerOfTwo(value), false, nameof(value));

            field = value;
            MakeSizedShadow(value, 20.0f, out var distance, out var constBias, out var  slopBias);
            IsDirty = true;
            HasPendingShadowSize = true;
            InvMapSize = 1.0f / value;
            Distance = distance;
            ConstBias = constBias;
            SlopeBias = slopBias;
        }
    }
    [InputNumber(InputStyle.Slider, Min = 10f, Max = 500f)]
    public float Distance     
    {
        get; set { field = value; IsDirty = true;  }
    }

    [InputNumber(InputStyle.Slider, Min = 10f, Max = 100f)]
    public float ZPad 
    {
        get; set { field = value; IsDirty = true;  }
    }

    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0.001f, Max = 0.01f, Format = "%.4f")]
    public float ConstBias     
    {
        get; set { field = value; IsDirty = true;  }
    }

    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0.001f, Max = 0.01f, Format = "%.4f")]
    public float SlopeBias     
    {
        get; set { field = value; IsDirty = true;  }
    }
    
    [InputNumber(InputStyle.Slider, Min = 0, Max = 1f)]
    public float Strength     
    {
        get; set { field = value; IsDirty = true;  }
    } = 1f;

    [InputNumber(InputStyle.Slider, Min = 0.5f, Max = 4f)]
    public float PcfRadius    
    {
        get; set { field = value; IsDirty = true;  }
    } = 1f;
    
    
    public static void MakeSizedShadow(int size, float zPad, out float distance, out float constBias, out float slopeBias)
    {
        //ArgumentOutOfRangeException.ThrowIfLessThan(size, RenderLimits.MinShadowMapSize);
        //ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RenderLimits.MaxShadowMapSize);

        //constBias
        // 4k-map =  0.0003 to 0.0005
        // 2k-map = 0.0001 to 0.0002

        //slopeBias
        // 2k-map = 0.0025 - 0.0035
        // 4k-map = 0.0015f-0.0025f

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

    }
}
