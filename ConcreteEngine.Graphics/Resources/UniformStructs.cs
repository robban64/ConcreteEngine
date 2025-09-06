using System.Numerics;

namespace ConcreteEngine.Graphics.Resources;

public record struct DirectionalLightUniform(int Direction, int Diffuse, int Specular, int Intensity)
{
    public const string DirectionUniform =  "uLight.direction";
    public const string DiffuseUniform = "uLight.diffuse";
    public const string SpecularUniform = "uLight.specular";
    public const string IntensityUniform = "uLight.intensity";
}

public record struct MaterialUniform(int Shininess, int SpecularStrength)
{
    public const string ShininessUniform =  "uMaterial.shininess";
    public const string SpecularStrengthUniform = "uMaterial.specularStrength";
}