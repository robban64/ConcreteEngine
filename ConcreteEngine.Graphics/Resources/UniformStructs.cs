using System.Numerics;

namespace ConcreteEngine.Graphics.Resources;

public record struct DirectionalLightUniform(int Direction, int Diffuse, int Specular, int Intensity)
{
    public const string DirectionUniform =  "uLight.direction";
    public const string DiffuseUniform = "uLight.diffuse";
    public const string SpecularUniform = "uLight.specular";
    public const string IntensityUniform = "uLight.intensity";

}