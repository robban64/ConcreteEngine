using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Engine.Assets.Descriptors;

internal struct MaterialImportData
{
    public Color4 Color;
    public Vector4 Specular;
    public float Opacity;
    public float SpecularFactor;
    public float Shininess;
}

internal struct MaterialImportProps
{
    public bool HasColor;
    public bool HasOpacity;
    public bool HasSpecularFactor;
    public bool HasSpecular;
    public bool HasShininess;
}