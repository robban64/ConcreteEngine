using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Visuals;
// @formatter:off

[Inspect]
public sealed class FogSettings : VisualStateObject
{
   
    //
    [InputColor]
    public Color4 FogColor
    {
        get; set{field = value; IsDirty = true;}
    } = new(0.70f, 0.89f, 0.68f);

    [InputNumber(InputStyle.Slider, Min = 100, Max = 1500)]
    public float Density    
    {
        get; set { field = value; IsDirty = true;  }
    } = 720f;

    [InputNumber(InputStyle.Slider, Min = -1000f, Max = 1000f)]
    public float BaseHeight    
    {
        get; set { field = value; IsDirty = true;  }
    }= 0f;

    [InputNumber(InputStyle.Slider, Min = 0.001f, Max = 10000.0f)]
    public float HeightFalloff    
    {
        get; set { field = value; IsDirty = true;  }
    } = 5200f;
    
    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0f, Max = 1f, Format = "%.3f")]
    public float Strength    
    {
        get; set { field = value; IsDirty = true;  }
    } = 1.05f;
    
    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0f, Max = 1f, Format = "%.3f")]
    public float Scattering   
    {
        get; set { field = value; IsDirty = true;  }
    } = 0.09f;

    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0f, Max = 1f, Format = "%.3f")]
    public float DistanceWeight   
    {
        get; set { field = value; IsDirty = true;  }
    } = 1f;

    [InputNumber(InputStyle.Drag, Speed = 0.001f, Min = 0f, Max = 1f, Format = "%.3f")]
    public float HeightWeight    
    {
        get; set { field = value; IsDirty = true;  }
    } = 0.85f;
    [InputNumber(InputStyle.Drag, Speed = 1f, Min = 1f, Max = 10000f, Format = "%.2f")]
    public float MaxDistance
    {
        get; set { field = value; IsDirty = true;  }
    } = 9500f;
}
