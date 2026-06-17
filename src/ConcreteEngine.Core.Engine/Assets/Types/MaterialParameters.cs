namespace ConcreteEngine.Core.Engine.Assets;

public record struct MaterialSurface
{
    public float Shininess { get; set => field = float.Clamp(value, 0f, 1f); }
    public float Roughness { get; set => field = float.Clamp(value, 0f, 1f); }
    public float Metallic { get; set => field = float.Clamp(value, 0f, 1f); }
    
    public MaterialSurface(float shininess = 0f, float roughness = 0f, float metallic = 0f)
    {
        Shininess = shininess;
        Roughness = roughness;
        Metallic = metallic;
    }

}